﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Giants.Launcher
{
    public partial class LauncherForm : Form
    {
        // Constant settings
        const string GAME_NAME = "Giants: Citizen Kabuto";
        const string GAME_PATH = "GiantsMain.exe";
        const string REGISTRY_KEY = @"HKEY_CURRENT_USER\Software\PlanetMoon\Giants";
        const string REGISTRY_VALUE = "DestDir";
		const string UPDATE_URL = @"https://google.com"; // update me

        string _commandLine = String.Empty;
        string _gamePath = null;
		Updater _Updater;

        public LauncherForm()
        {
            this.InitializeComponent();

            // Set window title
            this.Text = GAME_NAME;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

		private void btnPlay_Click(object sender, EventArgs e)
		{
			GameSettings.Save();

			foreach (string c in Environment.GetCommandLineArgs())
                this._commandLine = this._commandLine + c + " ";

            string commandLine = string.Format("{0} -launcher", this._commandLine.Trim());

			try
			{
				Process gameProcess = new Process();

                gameProcess.StartInfo.Arguments = commandLine;
				gameProcess.StartInfo.FileName = this._gamePath;
				gameProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(this._gamePath);
	
				gameProcess.Start();
				Application.Exit();
			}
			catch(Exception ex)
			{
				MessageBox.Show(string.Format("Failed to launch game process at: {0}. {1}", this._gamePath, ex.Message),
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

        private void btnOptions_Click(object sender, EventArgs e)
        {
            OptionsForm form = new OptionsForm(GAME_NAME + " Options", this._gamePath);

            //form.MdiParent = this;
			form.ShowDialog();
        }

        private void LauncherForm_Load(object sender, EventArgs e)
        {
            // Find the game executable, first looking for it relative to our current directory and then
            // using the registry path if that fails.
            this._gamePath = Path.GetDirectoryName(Application.ExecutablePath) + "\\" + GAME_PATH;
            if (!File.Exists(this._gamePath))
            {
                this._gamePath = (string)Registry.GetValue(REGISTRY_KEY, REGISTRY_VALUE, null);
                if (this._gamePath != null)
                    this._gamePath = Path.Combine(this._gamePath, GAME_PATH);

                if (this._gamePath == null || !File.Exists(this._gamePath))
                {
                    string message = string.Format(Resources.AppNotFound, GAME_NAME);
                    MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Exit();
					return;
                }
            }

			Version gameVersion = null;
			try
			{
				
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(this._gamePath);
				gameVersion = new Version(fvi.FileVersion.Replace(',', '.'));
			}
			finally
			{
				if (gameVersion == null)
				{
					string message = string.Format(Resources.AppNotFound, GAME_NAME);
					MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Application.Exit();
				}
			}

			// Read game settings from registry
			GameSettings.Load(this._gamePath);

			if ((int)GameSettings.Get("NoAutoUpdate") == 0)
			{
                // Check for updates
                this._Updater = new Updater(new Uri(UPDATE_URL), gameVersion);
                this._Updater.DownloadUpdateInfo(this.LauncherForm_DownloadCompletedCallback, this.LauncherForm_DownloadProgressCallback);
			}
        }

		private void LauncherForm_MouseDown(object sender, MouseEventArgs e)
		{
			// Force window to be draggable even though we have no menu bar
			if (e.Button == MouseButtons.Left)
			{
				NativeMethods.ReleaseCapture();
				NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
			}

		}

		private void LauncherForm_Shown(object sender, EventArgs e)
		{
            this.btnOptions.Visible = true;
            this.btnPlay.Visible = true;
            this.btnExit.Visible = true;

			// Play intro sound
			SoundPlayer player = new SoundPlayer(Resources.LauncherStart);
			player.Play();
		}

		private void LauncherForm_DownloadCompletedCallback(object sender, AsyncCompletedEventArgs e)
		{
			if (e.Cancelled)
				return;

            this.updateProgressBar.Value = 0;
            this.updateProgressBar.Visible = false;
            this.txtProgress.Visible = false;

			if (e.Error != null)
			{
				string errorMsg = string.Format(Resources.UpdateDownloadFailedText, e.Error.Message);
				MessageBox.Show(errorMsg, Resources.UpdateDownloadFailedTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			else
			{
				// Show "Download Complete" message, warn that we're closing
				MessageBox.Show(Resources.LauncherClosingText, Resources.LauncherClosingTitle);

				UpdateInfo updateInfo = (UpdateInfo)e.UserState;

				// Start the installer process
				Process updaterProcess = new Process();
				updaterProcess.StartInfo.FileName = Path.Combine(Path.GetTempPath(), updateInfo.FileName);
				updaterProcess.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

				if (updateInfo.UpdateType == UpdateType.Game)
				{
					// Default installation directory to current directory
					updaterProcess.StartInfo.Arguments = string.Format("/D {0}", Path.GetDirectoryName(Application.ExecutablePath));
				}
				else if (updateInfo.UpdateType == UpdateType.Launcher)
				{
					// Default installation directory to current directory and launch a silent install
					updaterProcess.StartInfo.Arguments = string.Format("/S /D {0}", Path.GetDirectoryName(Application.ExecutablePath));
				}

				updaterProcess.Start();

				Application.Exit();
				return;
			}
		}

		private void LauncherForm_DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
		{
            this.updateProgressBar.Visible = true;
            this.updateProgressBar.Value = e.ProgressPercentage;

			UpdateInfo info = (UpdateInfo)e.UserState;

            this.txtProgress.Visible = true;
            this.txtProgress.Text = string.Format(Resources.DownloadProgress, e.ProgressPercentage, (info.FileSize / 1024) / 1024);
		}
    }
}
