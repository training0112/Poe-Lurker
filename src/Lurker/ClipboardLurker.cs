﻿//-----------------------------------------------------------------------
// <copyright file="ClipboardLurker.cs" company="Wohs">
//     Missing Copyright information from a valid stylecop.json file.
// </copyright>
//-----------------------------------------------------------------------


namespace Lurker
{
    using Gma.System.MouseKeyHook;
    using Lurker.Patreon;
    using Lurker.Patreon.Events;
    using Lurker.Patreon.Models;
    using Lurker.Patreon.Parsers;
    using Lurker.Services;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using WindowsInput;
    using WK.Libraries.SharpClipboardNS;

    public class ClipboardLurker: IDisposable
    {
        #region Fields

        private InputSimulator _simulator;
        private ItemParser _itemParser = new ItemParser();
        private SettingsService _settingsService;
        private SharpClipboard _clipboardMonitor;
        private string _lastClipboardText = string.Empty;
        private IKeyboardMouseEvents _keyboardEvent;
        private bool _isPledging;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ClipboardLurker"/> class.
        /// </summary>
        public ClipboardLurker(SettingsService settingsService)
        {
            this.ClearClipboard();
            this._simulator = new InputSimulator();
            this._clipboardMonitor = new SharpClipboard();
            this._settingsService = settingsService;

            this._settingsService.OnSave += this.SettingsService_OnSave;
            this._keyboardEvent = Hook.GlobalEvents();
            this._clipboardMonitor.ClipboardChanged += this.ClipboardMonitor_ClipboardChanged;

#if (!DEBUG)
            this._keyboardEvent.MouseClick += this.KeyboardEvent_MouseClick;
#endif

            var service = new PatreonService();
            service.IsPledging().ContinueWith(t => 
            { 
                this._isPledging = t.Result;
                service.Dispose();
            });
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a new item is in the clipboard.
        /// </summary>
        public event EventHandler<PoeItem> Newitem;

        /// <summary>
        /// Creates new offer.
        /// </summary>
        public event EventHandler<string> NewOffer;

        #endregion

        #region Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._keyboardEvent.Dispose();
                this._clipboardMonitor.ClipboardChanged -= ClipboardMonitor_ClipboardChanged;
                this._settingsService.OnSave -= this.SettingsService_OnSave;
            }
        }


        /// <summary>
        /// Handles the OnSave event of the SettingsService control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private async void SettingsService_OnSave(object sender, EventArgs e)
        {
            using (var service = new PatreonService())
            {
                this._isPledging = await service.IsPledging();
            }
        }

        /// <summary>
        /// Handles the MouseClick event of the KeyboardEvent control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void KeyboardEvent_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Task.Run(() =>
            {
                if (!this._settingsService.SearchEnabled || !this._isPledging || e.Button != System.Windows.Forms.MouseButtons.Left)
                {
                    return;
                }

                if (Native.IsKeyPressed(Native.VirtualKeyStates.VK_SHIFT) && Native.IsKeyPressed(Native.VirtualKeyStates.VK_CONTROL))
                {
                    this.ParseItem();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the clipboard data.
        /// </summary>
        /// <returns>The clipboard text.</returns>
        private string GetClipboardText()
        {
            var clipboardText = string.Empty;
            Thread thread = new Thread(() => 
            {
                var retryCount = 3;
                while (retryCount != 0)
                {
                    try
                    {
                        clipboardText = Clipboard.GetText();
                        break;
                    }
                    catch
                    {
                        retryCount--;
                        Thread.Sleep(200);
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return clipboardText;
        }

        /// <summary>
        /// Clears the clipboard.
        /// </summary>
        private void ClearClipboard()
        {
            Thread thread = new Thread(() =>
            {
                var retryCount = 3;
                while (retryCount != 0)
                {
                    try
                    {
                        Clipboard.Clear();
                        break;
                    }
                    catch
                    {
                        retryCount--;
                        Thread.Sleep(200);
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Handles the ClipboardChanged event of the ClipboardMonitor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SharpClipboard.ClipboardChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void ClipboardMonitor_ClipboardChanged(object sender, SharpClipboard.ClipboardChangedEventArgs e)
        {
            if (e.ContentType == SharpClipboard.ContentTypes.Text)
            {
                var currentText = e.Content as string;
                if (string.IsNullOrEmpty(currentText))
                {
                    return;
                }

                if (this._lastClipboardText == currentText)
                {
                    return;
                }

                this._lastClipboardText = currentText;
                if (TradeEvent.IsTradeMessage(currentText))
                {
                    this.NewOffer?.Invoke(this, currentText);
                }
            }
        }

        /// <summary>
        /// Parses the item.
        /// </summary>
        private async void ParseItem()
        {
            PoeItem item = default;
            var retryCount = 2;
            for (int i = 0; i < retryCount; i++)
            {
                item = await this.GetItemInClipboard();
                if (item == null)
                {
                    return;
                }

                if (!item.Identified)
                {
                    await Task.Delay(50);
                    continue;
                }

                break;
            }

            if (item == null || !item.Identified)
            {
                return;
            }

            this.Newitem?.Invoke(this, item);
            this.ClearClipboard();
        }

        /// <summary>
        /// Gets the item in clipboard.
        /// </summary>
        /// <returns>The item in the clipboard</returns>
        private async Task<PoeItem> GetItemInClipboard()
        {
            this._simulator.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.VK_C);
            await Task.Delay(20);
            return this._itemParser.Parse(this.GetClipboardText());
        }

#endregion
    }
}
