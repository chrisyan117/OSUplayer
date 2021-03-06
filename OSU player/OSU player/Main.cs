﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Meebey.SmartIrc4net;
using OSUplayer.OsuFiles;
using OSUplayer.Properties;
using OSUplayer.Uilties;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace OSUplayer
{
    public partial class Main : RadForm
    {
        private enum NextMode
        {
            Main_Option_PlayMode_Normal = 1,
            Main_Option_PlayMode_Repeat = 2,
            Main_Option_PlayMode_Random = 3
        }

        private HotkeyHelper _hotkeyHelper;
        private int _nextKey;
        private int _nextKey1;
        private int _playKey;
        private int _playKey1;
        private int _playKey2;

        public Main()
        {
            InitializeComponent();
            Initlang();
        }

        #region 各种方法

        private void Initlang()
        {
            foreach (string lang in LanguageManager.LanguageList)
            {
                var item = new RadMenuItem { Text = lang };
                item.Click += delegate
                {
                    LanguageManager.Current = item.Text;
                    UpdateFormLanguage();
                };
                Main_LanguageSelect.Items.Add(item);
            }
            UpdateFormLanguage();
        }

        private void UpdateFormLanguage()
        {
            LanguageManager.ApplyLanguage(this);
            Main_QQ_Hint_Label.Text = LanguageManager.Get("Main_QQ_Hint_Label_Text") + Settings.Default.QQuin;
            Main_CurrentList.Text = LanguageManager.Get("Main_CurrentList_Text") + Core.CurrentListName;
        }

        private void TaskbarIconClickHandler(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button != MouseButtons.Left) return;
            if (Visible)
            {
                Visible = false;
            }
            else
            {
                Visible = true;
                WindowState = FormWindowState.Normal;
            }
        }

        private void AskForExit(object sender, FormClosingEventArgs e)
        {
            //Core.PauseOrResume();
            Core.Stop();
            if (RadMessageBox.Show(LanguageManager.Get("Comfirm_Exit_Text"), LanguageManager.Get("Tip_Text"), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Core.MainIsVisible = false;
                Core.Exit();
                _hotkeyHelper.UnregisterHotkeys();
                Dispose();
            }
            else
            {
                e.Cancel = true;
                //Core.PauseOrResume();
            }
        }

        private void SetDetail()
        {
            Main_ListDetail.Items.Clear();
            Main_ListDetail.Items.AddRange(Core.Getdetail());
            Core.SetBG();
        }

        private void Stop()
        {
            UpdateTimer.Enabled = false;
            Core.Stop();
            Main_Time_Trackbar.Enabled = false;
            Main_Time_Trackbar.Value = 0;
            Main_Stop.Enabled = false;
            Main_Play.Text = LanguageManager.Get("Main_Play_Text");
        }

        private void Play()
        {
            if (Core.PlayList.Count != 0)
            {
                UpdateTimer.Enabled = true;
                Core.Play();
                Main_PlayList.Refresh();
                Main_Time_Trackbar.Enabled = true;
                Main_Play.Text = LanguageManager.Get("Main_Pause_Text");
                Main_Stop.Enabled = true;
                Main_Time_Trackbar.Maximum = (int)Core.Durnation * 1000;
            }
        }

        private void Pause()
        {
            UpdateTimer.Enabled = false;
            Core.PauseOrResume();
            Main_Play.Text = LanguageManager.Get("Main_Play_Text");
        }

        private void Resume()
        {
            Core.PauseOrResume();
            UpdateTimer.Enabled = true;
            Main_Play.Text = LanguageManager.Get("Main_Pause_Text");
        }

        private void PlayNext(bool play = true)
        {
            if (Core.PlayList.Count == 0) return;
            var nextSongId = Core.GetNext();
            Main_PlayList.SelectedIndices.Clear();
            Main_PlayList.SelectedIndices.Add(nextSongId);
            Main_PlayList.EnsureVisible(nextSongId);
            Main_PlayList.Focus();
            SetDetail();
            if (play)
            {
                Play();
            }
        }

        private void Setscore()
        {
            Main_ScoreBox.Items.Clear();
            foreach (var item in Core.Getscore(Main_ScoreBox.Font).Where(item => item != null))
            {
                Main_ScoreBox.Items.Add(item);
            }
        }

        #endregion

        private void SetForm()
        {
            Main_QQ_Hint_Label.Text += Settings.Default.QQuin;
            Main_Option_Sync_QQ.IsChecked = Settings.Default.SyncQQ;
            Main_Volume_TrackBar.Value = 100 - (int)(Settings.Default.Allvolume * Main_Volume_TrackBar.Maximum);
            Main_Volume_Fx_TrackBar.Value = (int)(Settings.Default.Fxvolume * Main_Volume_Fx_TrackBar.Maximum);
            Main_Volume_Music_TrackBar.Value = (int)(Settings.Default.Musicvolume * Main_Volume_Music_TrackBar.Maximum);
            Main_Option_Play_Fx.IsChecked = Settings.Default.PlayFx;
            Main_Option_Play_SB.IsChecked = Settings.Default.PlaySB;
            Main_Option_Play_Video.IsChecked = Settings.Default.PlayVideo;
            Main_Option_Show_Popup.IsChecked = Settings.Default.ShowPopup;
            ((RadMenuItem)Main_Option_PlayMode.Items[Settings.Default.NextMode - 1]).IsChecked =
                true;
        }

        private void RefreshList(int select = 0)
        {
            //Main_PlayList.Items.Clear();
            Main_DiffList.Items.Clear();
            //PlayList.Enabled = false;
            //DiffList.Enabled = false;
            Main_PlayList.VirtualListSize = Core.PlayList.Count;
            Main_PlayList.SelectedIndices.Clear();
            Main_PlayList.Refresh();
            if (Core.PlayList.Count != 0) Main_PlayList.SelectedIndices.Add(select);
            /* new Thread(delegate()
            {
                foreach (int t in Core.PlayList)
                {
                    Main_PlayList.Invoke((MethodInvoker)(() =>
                        Main_PlayList.Items.Add(Core.Allsets[t].ToString())));
                }
                if (Main_PlayList.Items.Count != 0)
                {
                    Main_PlayList.Invoke((MethodInvoker)(() => Main_PlayList.Items[@select].Selected = true));
                }
                //PlayList.Enabled = true;
                //DiffList.Enabled = true;
            }).Start();*/
        }

        private void Main_VisibleChanged(object sender, EventArgs e)
        {
            Core.MainIsVisible = Visible;
        }

        private void Main_Mini_Switcher_Click(object sender, EventArgs e)
        {
            Visible = false;
            UpdateTimer.Enabled = false;
            _hotkeyHelper.UnregisterHotkeys();
            NotifySystem.RegisterClick(null);
            using (var dialog = new Mini())
            {
                dialog.ShowDialog();
            }
            NotifySystem.RegisterClick(TaskbarIconClickHandler);
            _playKey = _hotkeyHelper.RegisterHotkey(Keys.F5, KeyModifiers.Alt);
            _nextKey = _hotkeyHelper.RegisterHotkey(Keys.Right, KeyModifiers.Alt);
            _hotkeyHelper.OnHotkey += OnHotkey;
            Main_PlayList.SelectedIndices.Clear();
            int currentset = Core.PlayList.IndexOf(Core.currentset, 0);
            if (currentset == -1)
            {
                currentset = 0;
            }
            Main_PlayList.SelectedIndices.Add(currentset);
            //Main_PlayList.Items[currentset].Selected = true;
            Main_PlayList.EnsureVisible(currentset);
            Main_PlayList.Focus();
            Core.SetBG();
            if (Core.Isplaying)
            {
                Main_Time_Trackbar.Maximum = (int)Core.Durnation * 1000;
                Main_Time_Trackbar.Enabled = true;
                UpdateTimer.Enabled = true;
                Main_Play.Text = LanguageManager.Get("Main_Pause_Text");
                Main_Stop.Enabled = true;
            }
            else
            {
                Main_Time_Trackbar.Enabled = false;
                UpdateTimer.Enabled = false;
                Main_Play.Text = LanguageManager.Get("Main_Play_Text");
                Main_Stop.Enabled = false;
            }
            Visible = true;
        }

        private void Main_PageView_SelectedPageChanged(object sender, EventArgs e)
        {
            if (Main_PageView.SelectedPage == Main_PageView_Page2)
            {
                if (!Core.Scoresearched)
                {
                    string scorepath = Path.Combine(Settings.Default.OSUpath, "scores.db");
                    if (File.Exists(scorepath))
                    {
                        OsuDB.ReadScore(scorepath);
                        Core.Scoresearched = true;
                        Main_File_Import_Scores.Text = LanguageManager.Get("Main_File_Re_Import_Scores_Text");
                    }
                }
                Setscore();
            }
        }

        private void Main_Option_Select_QQ_Click(object sender, EventArgs e)
        {
            Core.SetQQ();
            Main_QQ_Hint_Label.Text = LanguageManager.Get("Main_QQ_Hint_Label_Text") + Settings.Default.QQuin;
            Main_Option_Sync_QQ.IsChecked = Settings.Default.SyncQQ;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Main_Time_Trackbar.Value = (int)Core.Position * 1000;
            Main_Time_Display.Text = String.Format("{0}:{1:D2} / {2}:{3:D2}", (int)Core.Position / 60,
                (int)Core.Position % 60, (int)Core.Durnation / 60,
                (int)Core.Durnation % 60);
            if (Core.Willnext)
            {
                Stop();
                PlayNext();
            }
        }

        private void NextTimer_Tick(object sender, EventArgs e)
        {
            NextTimer.Enabled = false;
            Stop();
            PlayNext();
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            SearchTimer.Enabled = false;
            Core.Search(Main_Search_Box.Text);
            RefreshList();
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            this.CenterToScreen();
            Core.Init(Main_Main_Display.Handle, Main_Main_Display.Size);
            SetForm();
            RefreshList();
            VisibleChanged += Main_VisibleChanged;
            SizeChanged += Main_SizeChanged;
            NotifySystem.RegisterClick(TaskbarIconClickHandler);
            Core.MainIsVisible = true;
            _hotkeyHelper = new HotkeyHelper(Handle);
            _playKey = _hotkeyHelper.RegisterHotkey(Keys.F5, KeyModifiers.Alt);
            _playKey1 = _hotkeyHelper.RegisterHotkey(Keys.Play, KeyModifiers.None);
            _playKey2 = _hotkeyHelper.RegisterHotkey(Keys.Pause, KeyModifiers.None);
            _nextKey = _hotkeyHelper.RegisterHotkey(Keys.Right, KeyModifiers.Alt);
            _nextKey1 = _hotkeyHelper.RegisterHotkey(Keys.MediaNextTrack, KeyModifiers.None);
            _hotkeyHelper.OnHotkey += OnHotkey;

            Main_Main_Display.ResumeLayout();
        }

        private void OnHotkey(int hotkeyID)
        {
            if (hotkeyID == _playKey || hotkeyID == _playKey1 || hotkeyID == _playKey2)
            {
                Main_Play.PerformClick();
            }
            else if (hotkeyID == _nextKey || hotkeyID == _nextKey1)
            {
                Main_PlayNext.PerformClick();
            }
        }

        private void Main_SizeChanged(object sender, EventArgs e)
        {
            if (Core.MainIsVisible)
            {
                Core.Resize(Main_Main_Display.Size);
            }
            if (WindowState == FormWindowState.Minimized)
            {
                Visible = false;
            }
        }

        private void Main_PlayList_RightClick_Menu_Opening(object sender, CancelEventArgs e)
        {
            if (Main_PlayList.SelectedIndices.Count == 0)
            {
                e.Cancel = true;
            }
        }

        private void Main_PlayList_RightClick_Delete_One_Click(object sender, EventArgs e)
        {
            int index = Main_PlayList.SelectedIndices[0];
            Core.Remove(index);
            if (Main_PlayList.SelectedIndices.Count != 0)
            {
                RefreshList(index);
            }
        }

        private void Main_Jump_OSU_Click(object sender, EventArgs e)
        {
            Core.Stop();
            using (var outStream = new FileStream("tmp.osr", FileMode.Create))
            {
                using (var wr = new BinaryWriter(outStream))
                {
                    wr.Write((byte)0);
                    wr.Write(20140127);
                    wr.Write((byte)0x0b);
                    wr.Write(Core.CurrentBeatmap.Hash);
                    wr.Write((byte)0x0b);
                    wr.Write("osu!");
                    byte[] res = MD5.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(string.Format(
                            PrivateConfig.ScoreHash, Core.CurrentBeatmap.Hash)));
                    var sb = new StringBuilder();
                    foreach (byte b in res)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    wr.Write((byte)0x0b);
                    wr.Write(sb.ToString());
                    wr.Write((UInt16)1);
                    wr.Write((UInt64)0);
                    wr.Write((UInt16)0);
                    wr.Write((UInt32)0);
                    wr.Write((UInt16)1);
                    wr.Write((byte)1);
                    wr.Write((UInt32)2048);
                    wr.Write((byte)0x0b);
                    wr.Write("");
                    wr.Write(new DateTime(2014, 1, 1).Ticks);
                    wr.Write(0);
                    wr.Write((UInt32)0);
                }
            }
            Process.Start("tmp.osr");
        }

        private void Main_Option_PlayMode_Click(object sender, EventArgs e)
        {
            var menu = (RadMenuItem)sender;
            if (menu.IsChecked) return;
            foreach (RadElement child in menu.Parent.Children)
            {
                ((RadMenuItem)child).IsChecked = false;
            }
            menu.IsChecked = true;
            Settings.Default.NextMode = (int)(NextMode)Enum.Parse(typeof(NextMode), menu.Name);
        }

        #region 菜单栏

        #region 文件

        private void Main_File_Run_OSU_Click(object sender, EventArgs e)
        {
            Process.Start(Path.Combine(Settings.Default.OSUpath, "osu!.exe"));
        }

        private void Main_File_Set_OSUPath_Click(object sender, EventArgs e)
        {
            if (!Core.Setpath()) return;
            //Main_PlayList.Items.Clear();
            Main_DiffList.Items.Clear();
            Core.RefreashSet();
            RefreshList();
        }

        private void Main_File_Import_OSU_Click(object sender, EventArgs e)
        {
            //Main_PlayList.Items.Clear();
            Main_DiffList.Items.Clear();
            Core.RefreashSet();
            RefreshList();
        }

        private void Main_File_Import_Scores_Click(object sender, EventArgs e)
        {
            Core.Scores.Clear();
            var scorepath = Path.Combine(Settings.Default.OSUpath, "scores.db");
            if (!File.Exists(scorepath)) return;
            OsuDB.ReadScore(scorepath);
            Core.Scoresearched = true;
            Main_File_Import_Scores.Text = LanguageManager.Get("Main_File_Re_Import_Scores_Text");
        }

        private void Main_File_Open_Folder_Click(object sender, EventArgs e)
        {
            Process.Start(Core.TmpSet.location);
        }

        private void Main_File_Open_MapFile_Click(object sender, EventArgs e)
        {
            Process.Start("notepad.exe", Core.TmpBeatmap.Path);
        }

        private void Main_File_Open_SBFile_Click(object sender, EventArgs e)
        {
            Process.Start("notepad.exe", Core.TmpSet.OsbPath);
        }

        private void Main_File_Export_Background_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Core.CurrentBeatmap.Background)) return;
            var dialog = new SaveFileDialog
            {
                CheckFileExists = false,
                CreatePrompt = false,
                AddExtension = true,
                OverwritePrompt = true,
                FileName = new FileInfo(Core.CurrentBeatmap.Background).Name,
                DefaultExt = new FileInfo(Core.CurrentBeatmap.Background).Extension,
                Filter = @"All files (*.*)|*.*",
                //InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (DialogResult.OK == dialog.ShowDialog())
            {
                File.Copy(Core.CurrentBeatmap.Background, dialog.FileName, true);
            }
        }

        private void Main_File_Export_MP3_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                CheckFileExists = false,
                CreatePrompt = false,
                AddExtension = true,
                OverwritePrompt = true,
                FileName = Core.CurrentBeatmap.Title,
                DefaultExt = new FileInfo(Core.CurrentBeatmap.Audio).Extension,
                Filter = @"All files (*.*)|*.*",
               // InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (DialogResult.OK == dialog.ShowDialog())
            {
                File.Copy(Core.CurrentBeatmap.Audio, dialog.FileName, true);
            }
        }

        private void Main_File_Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region 工具

        private void Main_Tool_Search_Dulplate_Click(object sender, EventArgs e)
        {
            using (var dialog = new DelDulp())
            {
                dialog.ShowDialog();
            }
        }

        #endregion

        #region 选项

        private void Main_Option_Play_Fx_Click(object sender, EventArgs e)
        {
            Settings.Default.PlayFx = Main_Option_Play_Fx.IsChecked;
        }

        private void Main_Option_Play_Video_Click(object sender, EventArgs e)
        {
            Settings.Default.PlayVideo = Main_Option_Play_Video.IsChecked;
        }

        private void Main_Option_Sync_QQ_Click(object sender, EventArgs e)
        {
            if (Settings.Default.SyncQQ && Settings.Default.QQuin != "0")
            {
                NotifySystem.ClearText();
            }
            Settings.Default.SyncQQ = Main_Option_Sync_QQ.IsChecked;
        }

        private void Main_Option_Play_SB_Click(object sender, EventArgs e)
        {
            Settings.Default.PlaySB = Main_Option_Play_SB.IsChecked;
        }

        private void Main_Option_Show_Popup_Click(object sender, EventArgs e)
        {
            Settings.Default.ShowPopup = Main_Option_Show_Popup.IsChecked;
        }

        #endregion

        private void Main_About_Click(object sender, EventArgs e)
        {
            using (var dialog = new About())
            {
                dialog.ShowDialog();
            }
        }

        #endregion

        #region 第一排

        private void Main_Collections_Click(object sender, EventArgs e)
        {
            //Visible = false;
            using (var dialog = new ChooseColl())
            {
                var center = this.Location;
                center.X = center.X + this.Width / 2 - dialog.Width / 2;
                center.Y = center.Y + this.Height / 2 - dialog.Height / 2;
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = center;
                dialog.ShowDialog();
            }
            //Visible = true;
            RefreshList();
            Main_CurrentList.Text = LanguageManager.Get("Main_CurrentList_Text") + Core.CurrentListName;
        }

        private void Main_Volume_Fx_TrackBar_Scroll(object sender, ScrollEventArgs e)
        {
            Core.SetVolume(3, Main_Volume_Fx_TrackBar.Value / (float)Main_Volume_Fx_TrackBar.Maximum);
        }

        private void Main_Volume_Music_TrackBar_Scroll(object sender, ScrollEventArgs e)
        {
            Core.SetVolume(2, Main_Volume_Music_TrackBar.Value / (float)Main_Volume_Music_TrackBar.Maximum);
        }

        private void Main_Volume_TrackBar_Scroll(object sender, ScrollEventArgs e)
        {
            Core.SetVolume(1, 1.0f - Main_Volume_TrackBar.Value / (float)Main_Volume_TrackBar.Maximum);
        }

        #endregion

        #region 第二排

        private void Main_PlayList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Main_PlayList.SelectedIndices.Count == 0)
            {
                return;
            }
            Main_DiffList.Items.Clear();
            if (Core.SetSet(Main_PlayList.SelectedIndices[0]))
            {
                RefreshList();
                PlayNext(false);
            }
            else
            {
                foreach (var s in Core.TmpSet.Diffs)
                {
                    Main_DiffList.Items.Add(s.Version);
                }
                Main_File_Open_SBFile.Enabled = File.Exists(Core.TmpSet.OsbPath);
                Main_DiffList.SelectedIndex = 0;
            }
        }

        private void Main_DiffList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Main_PlayList.SelectedIndices.Count == 0)
            {
                return;
            }
            if (Core.SetMap(Main_DiffList.SelectedIndex))
            {
                RefreshList();
                PlayNext(false);
            }
            else
            {
                if (Core.Scoresearched)
                {
                    Setscore();
                }
                if (!Main_Stop.Enabled)
                {
                    Core.currentset = Core.PlayList[Core.tmpset];
                    Core.currentmap = Core.tmpmap;
                    SetDetail();
                }
                else if (Core.Isplaying)
                {
                    Main_ListDetail.Items.Clear();
                    Main_ListDetail.Items.AddRange(Core.Getdetail());
                }
                else
                {
                    Stop();
                    SetDetail();
                }
            }
        }

        private void Main_PlayList_DoubleClick(object sender, EventArgs e)
        {
            if (Core.SetSet(Main_PlayList.SelectedIndices[0], true))
            {
                RefreshList();
                PlayNext();
            }
            else
            {
                Core.SetMap(0, true);
                Stop();
                SetDetail();
                Play();
            }
        }

        private void Main_DiffList_DoubleClick(object sender, EventArgs e)
        {
            if (Core.SetMap(Main_DiffList.SelectedIndex, true))
            {
                RefreshList();
                PlayNext();
            }
            else
            {
                Stop();
                SetDetail();
                Play();
            }
        }

        private void Main_Stop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void Main_PlayNext_Click(object sender, EventArgs e)
        {
            NextTimer.Enabled = false;
            NextTimer.Enabled = true;
        }

        private void Main_Time_Trackbar_Scroll(object sender, ScrollEventArgs e)
        {
            Core.Seek((double)Main_Time_Trackbar.Value / 1000);
        }

        private void Main_Search_Box_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape)
            {
                Main_Search_Box.Text = "";
                Core.Search(Main_Search_Box.Text);
                RefreshList();
            }
            if (e.KeyChar == (char)13)
            {
                Core.Search(Main_Search_Box.Text);
                RefreshList();
            }
            else
            {
                SearchTimer.Enabled = false;
                SearchTimer.Enabled = true;
            }
        }

        private void Main_Play_Click(object sender, EventArgs e)
        {
            if (!Main_Stop.Enabled)
            {
                Play();
            }
            else
            {
                if (Main_Play.Text == LanguageManager.Get("Main_Play_Text"))
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        #endregion

        private void Main_PlayList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (Core.PlayList == null || Core.PlayList.Count == 0)
            {
                e.Item = new ListViewItem();
                return;
            }

            if (e.ItemIndex < Core.PlayList.Count)
            {
                var item = new ListViewItem(Core.Allsets[Core.PlayList[e.ItemIndex]].ToString())
                {
                    ForeColor = e.ItemIndex == Core.currentset ? Color.White : Color.Black,
                    Font = e.ItemIndex == Core.currentset ? new Font("宋体", 9, FontStyle.Bold) : new Font("宋体", 9),
                    BackColor =
                        e.ItemIndex % 2 == 0
                            ? (e.ItemIndex == Core.currentset ? Color.LightSkyBlue : Color.White)
                            : (e.ItemIndex == Core.currentset ? Color.DeepSkyBlue : Color.WhiteSmoke)
                };
                e.Item = item;
            }
            else
            {
                e.Item = new ListViewItem();
            }
        }

        private void Main_Tool_Export_Playlist_Click(object sender, EventArgs e)
        {

        }

        private void Main_Tool_Export_Playlist_MP3_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (DialogResult.OK != dialog.ShowDialog()) return;
            foreach (var song in Core.PlayList)
            {
                Core.Allsets[song].SaveAudios(dialog.SelectedPath);
            }
            NotifySystem.Showtip(1000, LanguageManager.Get("OSUplayer"), LanguageManager.Get("Save_Complete"));
        }

        private void Main_PlayList_RightClick_Copy_Current_Name_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(Core.TmpSet.ToString());
        }
    }
}