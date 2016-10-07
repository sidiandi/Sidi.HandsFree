// Copyright (c) 2016, Andreas Grimme

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sidi.HandsFree
{
    /// <summary>
    /// AT command connection according to AT command set for User Equipment (UE) (3GPP TS 27.007 version 6.8.0 Release 6)
    /// </summary>
    public class AtCommandConnection
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Stream stream;

        public AtCommandConnection(Stream stream)
        {
            this.stream = stream;
            Task.Factory.StartNew(ReceiveResponses, TaskCreationOptions.LongRunning);
        }

        async Task ReceiveResponses()
        {
            for (;;)
            {
                var r = await ReadResponse();
                if (r == null)
                {
                    break;
                }
                OnResponse(r);
            }
        }

        void OnResponse(string response)
        {
            if (Response != null)
            {
                Response(this, response);
            }
        }
        List<string> responses = null;

        public event EventHandler<string> Response;
        object sendCommand = new object();

        class CommandResponseCollector
        {
            public CommandResponseCollector(AtCommandConnection at)
            {
                this.at = at;
            }

            readonly AtCommandConnection at;

            List<string> responses = new List<string>();
            string status;

            public IList<string> Responses { get { return responses; } } 
            public string Status { get { return status; } }

            public void Collect()
            {
                at.Response += At_Response;
                lock (this)
                {
                    for (; status == null;)
                    {
                        Monitor.Wait(this);
                    }
                }
                at.Response -= At_Response;
            }

            private void At_Response(object sender, string e)
            {
                lock (this)
                {
                    if (IsOk(e) || IsError(e))
                    {
                        status = e;
                        Monitor.PulseAll(this);
                    }
                    else
                    {
                        responses.Add(e);
                    }
                }
            }
        }

        public async Task<string[]> Command(string commandLine)
        {
            await Write(commandPrefix + commandLine + commandPostfix);
            var cr = new CommandResponseCollector(this);
            cr.Collect();
            if (IsError(cr.Status))
            {
                throw new AtCommandException(cr.Status);
            }
            return cr.Responses.ToArray();
        }

        static bool IsOk(string response)
        {
            return string.Equals(response, "OK");
        }

        static bool IsError(string response)
        {
            return response.StartsWith("+CME ERROR:") || string.Equals(response, "ERROR");
        }

        async Task Write(string text)
        {
            var buffer = ASCIIEncoding.ASCII.GetBytes(text);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            await stream.FlushAsync();
            log.DebugFormat("Write: {0}", text);
        }

        internal void Close()
        {
            stream.Close();
        }

        const string commandPrefix = "AT";
        const string commandPostfix = "\r";
        const string responsePrefix = "\r\n";
        const string responsePostfix = "\r\n";

        async Task<string> ReadResponse()
        {
            var prefix = await ReadUntilAsync(responsePrefix);
            if (prefix == null)
            {
                return null;
            }

            return await ReadUntilAsync(responsePostfix);
        }

        async Task<string> ReadUntilAsync(string terminationString)
        {
            int terminationStringIndex = 0;
            using (var w = new StringWriter())
            {
                for (;;)
                {
                    var c = stream.ReadByte();
                    if (c < 0)
                    {
                        return null;
                    }
                    w.Write((char)c);
                    if (terminationString[terminationStringIndex] == (char)c)
                    {
                        terminationStringIndex++;
                    }
                    else
                    {
                        terminationStringIndex = 0;
                    }

                    if (terminationStringIndex >= terminationString.Length)
                    {
                        break;
                    }
                }
                var line = w.ToString();
                line = line.Substring(0, line.Length - terminationString.Length);
                log.DebugFormat("Read: {0}", line);
                return line;
            }
        }

        public async Task<string> Get(string command)
        {
            var p = Regex.Replace(command, "[?=]+$", String.Empty) + ": ";
            foreach (var r in await Command(command))
            {
                if (r.StartsWith(p))
                {
                    return r.Substring(p.Length);
                }
            }
            return null;
        }
    }
}
