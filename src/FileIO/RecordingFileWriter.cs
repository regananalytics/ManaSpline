using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ManaSpline
{
    public class RecordingFileWriter : IDisposable
    {
        private readonly string _filePath;
        private readonly BlockingCollection<string> _writeQueue = new BlockingCollection<string>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public Task _fileWriterTask;
        private bool _disposed = false;

        public bool IsWriting { get; private set; } = false;

        public RecordingFileWriter(string filePath)
        {
            _filePath = filePath;

            BackupExistingFile();

            _fileWriterTask = Task.Run(() => ProcessWriteQueueAsync(), _cts.Token);
        }

        // Enqueue a new record to be written
        public async Task EnqueueRecord(string json)
        {
            if (!_disposed)
                _writeQueue.Add(json);
        }

        // Process the write queue and ensure records are written in order
        private async Task ProcessWriteQueueAsync()
        {
            IsWriting = true;
            try
            {
                using (var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream))
                {
                    foreach (var json in _writeQueue.GetConsumingEnumerable())
                    {
                        await writer.WriteLineAsync(json);
                        if (ManaSpline.Verbose)
                            ManaSpline.Log(json);
                    }
                }
            }
            catch (Exception ex)
            {
                ManaSpline.Log($"Exception raised in ProcessWriteQueue: {ex.Message}");
            }
            finally
            {
                IsWriting = false;
            }
        }

        // Backup the existing file if it exists
        private void BackupExistingFile()
        {
            if (File.Exists(_filePath))
            {
                string backupFile = _filePath + ".backup";
                File.Copy(_filePath, backupFile, overwrite: true);
                File.Delete(_filePath);
            }
        }

        // Complete adding records and wait for the writer task to finish
        public async Task CompleteAsync()
        {
            _writeQueue.CompleteAdding();
            _cts.Cancel();
        }

        // Ensure the writer is disposed and any remaining records are flushed
        public void Dispose()
        {
            if (_disposed)
            {
                _disposed = true;
                _cts.Cancel();
                _writeQueue.CompleteAdding();
                _fileWriterTask?.Wait();
                _cts.Dispose();
                _writeQueue.Dispose();
            }
        }
    }
 
    public class RecordPostProcessor
    {
        private readonly string _filePath;

        public RecordPostProcessor(string filePath)
        {
            _filePath = filePath;
        }

        public void Process()
        {
            ManaSpline.Log("Post-Processing File...");
            
            // Read and parse records from the file
            var records = ReadRecords();

            // Sort records by IGT
            var sortedRecords = records.OrderBy(r => r.IGT).ToList();

            // Write the sorted records back to the file
            WriteRecords(sortedRecords);

            ManaSpline.Log("Post-Processing Complete!");
        }

        private IEnumerable<Record> ReadRecords()
        {
            using (var reader = new StreamReader(_filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        Record record = null;
                        try
                        {
                            record = JsonConvert.DeserializeObject<Record>(line, new RecordConverter());
                        }
                        catch (JsonException ex)
                        {
                            ManaSpline.Log($"Error parsing line: {ex.Message}");
                            continue;
                        }

                        if (record != null)
                            yield return record;
                    }
                }
            }
        }

        private void WriteRecords(IEnumerable<Record> records)
        {
            string tempFile = _filePath + ".temp";

            using (var writer = new StreamWriter(tempFile))
            {
                foreach (var record in records)
                {
                    string json = JsonConvert.SerializeObject(record, Formatting.None);
                    writer.WriteLine(json);
                }
            }

            // Backup the original file
            string backupFile = _filePath + ".backup";
            if (File.Exists(_filePath))
            {
                File.Copy(_filePath, backupFile, overwrite: true);
                File.Delete(_filePath);
            }

            // Replace the original file with the sorted one
            File.Move(tempFile, _filePath);
        }

        private class RecordConverter : JsonConverter<Record>
        {
            public override Record ReadJson(JsonReader reader, Type objectType, Record existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject obj = JObject.Load(reader);

                var record = new Record
                {
                    Data = new Dictionary<string, object>()
                };

                foreach (var property in obj.Properties())
                {
                    if (property.Name == "IGT")
                    {
                        record.IGT = property.Value.Value<float>();
                    }
                    else
                    {
                        record.Data.Add(property.Name, property.Value.ToObject<object>());
                    }
                }

                return record;
            }

            public override void WriteJson(JsonWriter writer, Record value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("IGT");
                writer.WriteValue(value.IGT);

                foreach (var kvp in value.Data)
                {
                    writer.WritePropertyName(kvp.Key);
                    serializer.Serialize(writer, kvp.Value);
                }

                writer.WriteEndObject();
            }
        }

        public class Record
        {
            public float IGT { get; set; }
            public Dictionary<string, object> Data { get; set; }
        }
    }
}