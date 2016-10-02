// Copyright (c) 2016, Andreas Grimme

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidi.HandsFree
{
    /// <summary>
    /// AT command connection according to AT command set for User Equipment (UE) (3GPP TS 27.007 version 6.8.0 Release 6)
    /// </summary>
    class AtCommandConnection
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Stream stream;

        public AtCommandConnection(Stream stream)
        {
            this.stream = stream;
        }

        public async Task<string[]> Command(string commandLine)
        {
            await Write(commandPrefix + commandLine + commandPostfix);
            var responses = new List<string>();
            for (;;)
            {
                var r = await ReadResponse();
                if (IsOk(r))
                {
                    return responses.ToArray();
                }
                if (IsError(r))
                {
                    throw new AtCommandException(r);
                }
                responses.Add(r);
            }
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

        const string commandPrefix = "AT";
        const string commandPostfix = "\r";
        const string responsePrefix = "\r\n";
        const string responsePostfix = "\r\n";

        async Task<string> ReadResponse()
        {
            await ReadUntilAsync(responsePrefix);
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
                        throw new AtCommandException();
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
            var p = command + ": ";
            foreach (var r in await Command(command))
            {
                if (r.StartsWith(p))
                {
                    return r.Substring(p.Length);
                }
            }
            throw new AtCommandException();
        }
    }
}
