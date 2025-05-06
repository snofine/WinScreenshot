using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Screenshoter
{
    public enum ScreenshotType
    {
        SelectedArea,
        FullScreen,
        ActiveWindow
    }

    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isCapturing = false;
        private Rectangle captureArea;
        private Point startPoint;
        private Form overlayForm;
        private string saveFolder;
        private List<HotKey> hotKeys = new List<HotKey>();
        private bool copyToClipboard = true;
        private Bitmap fullScreenshot;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private class HotKey
        {
            public int Id { get; set; }
            public Keys Key { get; set; }
            public int Modifiers { get; set; }
            public string Description { get; set; }
            public ScreenshotType ScreenshotType { get; set; }
        }

        // Вспомогательный класс для двойной буферизации
        private class BufferedForm : Form
        {
            public BufferedForm()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }

        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            
            // Добавляем горячие клавиши по умолчанию
            AddHotKey(Keys.PrintScreen, 0, "Скриншот выделенной области", ScreenshotType.SelectedArea);
        }

        private void AddHotKey(Keys key, int modifiers, string description, ScreenshotType screenshotType)
        {
            int id = hotKeys.Count + 1;
            hotKeys.Add(new HotKey { Id = id, Key = key, Modifiers = modifiers, Description = description, ScreenshotType = screenshotType });
            RegisterHotKey(this.Handle, id, modifiers, (int)key);
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Сделать скриншот", null, OnCaptureClick);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Настройки", null, OnSettingsClick);
            trayMenu.Items.Add("Выход", null, OnExitClick);

            trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            using (var settingsForm = new Form())
            {
                settingsForm.Text = "Настройки";
                settingsForm.Size = new Size(500, 400);
                settingsForm.StartPosition = FormStartPosition.CenterScreen;
                settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                settingsForm.MaximizeBox = false;
                settingsForm.MinimizeBox = false;

                var folderLabel = new Label
                {
                    Text = "Папка для сохранения:",
                    Location = new Point(10, 20),
                    AutoSize = true
                };

                var folderTextBox = new TextBox
                {
                    Text = saveFolder,
                    Location = new Point(10, 40),
                    Width = 380
                };

                var browseButton = new Button
                {
                    Text = "Обзор...",
                    Location = new Point(400, 38),
                    Width = 70
                };

                var hotKeysLabel = new Label
                {
                    Text = "Горячие клавиши:",
                    Location = new Point(10, 70),
                    AutoSize = true
                };

                var hotKeysListBox = new ListBox
                {
                    Location = new Point(10, 90),
                    Size = new Size(380, 150)
                };

                foreach (var hotKey in hotKeys)
                {
                    hotKeysListBox.Items.Add($"{hotKey.Description}: {GetHotKeyString(hotKey)}");
                }

                var addHotKeyButton = new Button
                {
                    Text = "Добавить",
                    Location = new Point(10, 250),
                    Width = 100
                };

                var removeHotKeyButton = new Button
                {
                    Text = "Удалить",
                    Location = new Point(120, 250),
                    Width = 100
                };

                var copyToClipboardCheckBox = new CheckBox
                {
                    Text = "Копировать в буфер обмена",
                    Location = new Point(10, 290),
                    AutoSize = true,
                    Checked = copyToClipboard
                };

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(200, 320)
                };

                browseButton.Click += (s, ev) =>
                {
                    using (var dialog = new FolderBrowserDialog())
                    {
                        dialog.SelectedPath = saveFolder;
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            folderTextBox.Text = dialog.SelectedPath;
                        }
                    }
                };

                addHotKeyButton.Click += (s, ev) =>
                {
                    using (var hotKeyForm = new Form())
                    {
                        hotKeyForm.Text = "Добавить горячую клавишу";
                        hotKeyForm.Size = new Size(400, 300);
                        hotKeyForm.StartPosition = FormStartPosition.CenterParent;
                        hotKeyForm.FormBorderStyle = FormBorderStyle.Sizable;
                        hotKeyForm.MinimumSize = new Size(400, 300);
                        hotKeyForm.MaximizeBox = true;
                        hotKeyForm.MinimizeBox = true;

                        var descriptionLabel = new Label
                        {
                            Text = "Описание:",
                            Location = new Point(20, 20),
                            AutoSize = true
                        };

                        var descriptionTextBox = new TextBox
                        {
                            Location = new Point(20, 40),
                            Width = 340,
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                        };

                        var typeLabel = new Label
                        {
                            Text = "Тип скриншота:",
                            Location = new Point(20, 80),
                            AutoSize = true
                        };

                        var typeComboBox = new ComboBox
                        {
                            Location = new Point(20, 100),
                            Width = 340,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                        };
                        typeComboBox.Items.AddRange(new object[] {
                            "Выделенная область",
                            "Весь экран",
                            "Активное окно"
                        });
                        typeComboBox.SelectedIndex = 0;

                        var keyLabel = new Label
                        {
                            Text = "Нажмите комбинацию клавиш:",
                            Location = new Point(20, 140),
                            AutoSize = true
                        };

                        var keyTextBox = new TextBox
                        {
                            Location = new Point(20, 160),
                            Width = 340,
                            ReadOnly = true,
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                        };

                        var addButton = new Button
                        {
                            Text = "Добавить",
                            DialogResult = DialogResult.OK,
                            Location = new Point(150, 200),
                            Width = 100,
                            Height = 30,
                            Anchor = AnchorStyles.Top
                        };

                        Keys newKey = Keys.None;
                        int newModifiers = 0;

                        keyTextBox.KeyDown += (sender, args) =>
                        {
                            if (args.KeyCode != Keys.ControlKey && 
                                args.KeyCode != Keys.Alt && 
                                args.KeyCode != Keys.ShiftKey)
                            {
                                newKey = args.KeyCode;
                                newModifiers = 0;
                                if ((Control.ModifierKeys & Keys.Control) != 0) newModifiers |= 0x0002;
                                if ((Control.ModifierKeys & Keys.Alt) != 0) newModifiers |= 0x0001;
                                if ((Control.ModifierKeys & Keys.Shift) != 0) newModifiers |= 0x0004;
                                keyTextBox.Text = GetHotKeyString(newKey, newModifiers);
                                args.Handled = true;
                            }
                        };

                        hotKeyForm.Controls.AddRange(new Control[] { 
                            descriptionLabel, descriptionTextBox,
                            typeLabel, typeComboBox,
                            keyLabel, keyTextBox, addButton 
                        });

                        if (hotKeyForm.ShowDialog() == DialogResult.OK && 
                            !string.IsNullOrWhiteSpace(descriptionTextBox.Text) && 
                            newKey != Keys.None)
                        {
                            ScreenshotType screenshotType = (ScreenshotType)typeComboBox.SelectedIndex;
                            AddHotKey(newKey, newModifiers, descriptionTextBox.Text, screenshotType);
                            hotKeysListBox.Items.Add($"{descriptionTextBox.Text}: {GetHotKeyString(newKey, newModifiers)}");
                        }
                    }
                };

                removeHotKeyButton.Click += (s, ev) =>
                {
                    if (hotKeysListBox.SelectedIndex >= 0)
                    {
                        var hotKey = hotKeys[hotKeysListBox.SelectedIndex];
                        UnregisterHotKey(this.Handle, hotKey.Id);
                        hotKeys.RemoveAt(hotKeysListBox.SelectedIndex);
                        hotKeysListBox.Items.RemoveAt(hotKeysListBox.SelectedIndex);
                    }
                };

                settingsForm.Controls.AddRange(new Control[] { 
                    folderLabel, folderTextBox, browseButton,
                    hotKeysLabel, hotKeysListBox, addHotKeyButton, removeHotKeyButton,
                    copyToClipboardCheckBox, okButton 
                });

                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    saveFolder = folderTextBox.Text;
                    copyToClipboard = copyToClipboardCheckBox.Checked;
                }
            }
        }

        private string GetHotKeyString(Keys key, int modifiers)
        {
            string result = "";
            if ((modifiers & 0x0002) != 0) result += "Ctrl + ";
            if ((modifiers & 0x0001) != 0) result += "Alt + ";
            if ((modifiers & 0x0004) != 0) result += "Shift + ";
            return result + key.ToString();
        }

        private string GetHotKeyString(HotKey hotKey)
        {
            return GetHotKeyString(hotKey.Key, hotKey.Modifiers);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                var hotKey = hotKeys.Find(h => h.Id == id);
                if (hotKey != null)
                {
                    switch (hotKey.ScreenshotType)
                    {
                        case ScreenshotType.SelectedArea:
                            OnCaptureClick(this, EventArgs.Empty);
                            break;
                        case ScreenshotType.FullScreen:
                            CaptureFullScreen();
                            break;
                        case ScreenshotType.ActiveWindow:
                            CaptureActiveWindow();
                            break;
                    }
                }
            }
            base.WndProc(ref m);
        }

        private void OnCaptureClick(object sender, EventArgs e)
        {
            this.Hide();
            Thread.Sleep(200);

            // Делаем скриншот всего экрана заранее
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;
            fullScreenshot = new Bitmap(screenBounds.Width, screenBounds.Height);
            using (Graphics g = Graphics.FromImage(fullScreenshot))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, screenBounds.Size);
            }

            overlayForm = new BufferedForm
            {
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                TopMost = true,
                Cursor = Cursors.Cross,
                BackColor = Color.Black,
                Opacity = 1.0,
                ShowInTaskbar = false
            };

            overlayForm.MouseDown += (s, ev) =>
            {
                isCapturing = true;
                startPoint = ev.Location;
                captureArea = new Rectangle(ev.Location, new Size(0, 0));
                overlayForm.Invalidate();
            };

            overlayForm.MouseMove += (s, ev) =>
            {
                if (isCapturing)
                {
                    var width = ev.X - startPoint.X;
                    var height = ev.Y - startPoint.Y;
                    captureArea = new Rectangle(
                        Math.Min(startPoint.X, ev.X),
                        Math.Min(startPoint.Y, ev.Y),
                        Math.Abs(width),
                        Math.Abs(height)
                    );
                    overlayForm.Invalidate();
                }
            };

            overlayForm.MouseUp += (s, ev) =>
            {
                isCapturing = false;
                overlayForm.Close();
                CaptureArea(captureArea);
                this.Show();
            };

            overlayForm.Paint += (s, ev) =>
            {
                // Нарисовать скриншот с затемнением
                if (fullScreenshot != null)
                {
                    ev.Graphics.DrawImage(fullScreenshot, 0, 0);
                    using (Brush darkBrush = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                    {
                        ev.Graphics.FillRectangle(darkBrush, ev.ClipRectangle);
                    }
                    if (isCapturing && captureArea.Width > 0 && captureArea.Height > 0)
                    {
                        // Вырезать выделенную область и нарисовать её без затемнения
                        ev.Graphics.SetClip(captureArea);
                        ev.Graphics.DrawImage(fullScreenshot, 0, 0);
                        ev.Graphics.ResetClip();
                        // Нарисовать рамку
                        using (Pen pen = new Pen(Color.Red, 2))
                        {
                            ev.Graphics.DrawRectangle(pen, captureArea);
                        }
                    }
                }
            };

            overlayForm.ShowDialog();
        }

        private void CaptureArea(Rectangle area)
        {
            if (fullScreenshot != null && area.Width > 0 && area.Height > 0)
            {
                using (Bitmap bmp = new Bitmap(area.Width, area.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.DrawImage(fullScreenshot, new Rectangle(0, 0, area.Width, area.Height), area, GraphicsUnit.Pixel);
                    }
                    SaveOrCopy(bmp);
                }
            }
        }

        private void CaptureFullScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            using (Bitmap bmp = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                SaveOrCopy(bmp);
            }
        }

        private void CaptureActiveWindow()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd != IntPtr.Zero && GetWindowRect(hwnd, out RECT rect))
            {
                Rectangle area = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                using (Bitmap bmp = new Bitmap(area.Width, area.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(new Point(area.Left, area.Top), Point.Empty, area.Size);
                    }

                    SaveOrCopy(bmp);
                }
            }
        }

        private void SaveOrCopy(Bitmap bmp)
        {
            string filename = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = System.IO.Path.Combine(saveFolder, filename);

            bmp.Save(path, ImageFormat.Png);

            if (copyToClipboard)
            {
                Clipboard.SetImage(bmp);
            }

            trayIcon.ShowBalloonTip(2000, "Скриншот сделан", $"Сохранено: {filename}", ToolTipIcon.Info);
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            foreach (var hotKey in hotKeys)
            {
                UnregisterHotKey(this.Handle, hotKey.Id);
            }

            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
