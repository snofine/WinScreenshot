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

            overlayForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                TopMost = true,
                Cursor = Cursors.Cross,
                BackColor = Color.White,
                Opacity = 0.01
            };

            overlayForm.MouseDown += OverlayForm_MouseDown;
            overlayForm.MouseMove += OverlayForm_MouseMove;
            overlayForm.MouseUp += OverlayForm_MouseUp;
            overlayForm.KeyDown += OverlayForm_KeyDown;
            overlayForm.Paint += OverlayForm_Paint;

            overlayForm.Show();
        }

        private void OverlayForm_Paint(object sender, PaintEventArgs e)
        {
            if (isCapturing && captureArea.Width > 0 && captureArea.Height > 0)
            {
                // Рисуем тёмную заливку
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(brush, captureArea);
                }
                
                // Рисуем внешнюю подсветку (белую)
                using (Pen outerGlowPen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawRectangle(outerGlowPen, captureArea);
                }
                
                // Рисуем основную рамку (чёрную)
                using (Pen pen = new Pen(Color.Black, 2))
                {
                    e.Graphics.DrawRectangle(pen, captureArea);
                }
            }
        }

        private void OverlayForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isCapturing = true;
                startPoint = e.Location;
                captureArea = new Rectangle();
            }
        }

        private void OverlayForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isCapturing)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);

                captureArea = new Rectangle(x, y, width, height);
                overlayForm.Invalidate();
            }
        }

        private void OverlayForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isCapturing)
            {
                isCapturing = false;
                if (captureArea.Width > 0 && captureArea.Height > 0)
                {
                    overlayForm.Hide();
                    Thread.Sleep(100);
                    CaptureSelectedArea();
                    overlayForm.Close();
                }
                else
                {
                    overlayForm.Close();
                }
            }
        }

        private void OverlayForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                overlayForm.Close();
            }
        }

        private void CaptureFullScreen()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(screenshot))
                    {
                        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                    }

                    SaveScreenshot(screenshot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании скриншота: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CaptureActiveWindow()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                RECT rect;
                GetWindowRect(handle, out rect);
                Rectangle bounds = new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

                using (Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(screenshot))
                    {
                        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                    }

                    SaveScreenshot(screenshot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании скриншота: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveScreenshot(Bitmap screenshot)
        {
            if (copyToClipboard)
            {
                Clipboard.SetImage(screenshot);
            }

            string fileName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            string filePath = Path.Combine(saveFolder, fileName);

            screenshot.Save(filePath, ImageFormat.Png);
            MessageBox.Show($"Скриншот сохранен: {filePath}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CaptureSelectedArea()
        {
            try
            {
                using (Bitmap screenshot = new Bitmap(captureArea.Width, captureArea.Height))
                {
                    using (Graphics graphics = Graphics.FromImage(screenshot))
                    {
                        graphics.CopyFromScreen(captureArea.Left, captureArea.Top, 0, 0, captureArea.Size);
                    }

                    SaveScreenshot(screenshot);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании скриншота: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (var hotKey in hotKeys)
            {
                UnregisterHotKey(this.Handle, hotKey.Id);
            }
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
} 