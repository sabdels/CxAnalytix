﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CxRestClient
{
    public class CxScaScans
    {

        private static String URL_SUFFIX = "cxrestapi/osa/scans";

        private CxScaScans()
        { }

        public struct Scan
        {
            public int ProjectId { get; internal set; }
            public String ScanId { get; internal set; }
            public DateTime FinishTime { get; internal set; }
            public DateTime StartTime { get; internal set; }

        }


        private class ScansReader : IEnumerable<Scan>, IEnumerator<Scan>
        {

            private JToken _json;
            private JTokenReader _reader;
            private int _projectId;
            internal ScansReader(JToken json, int projectId)
            {
                _json = json;
                _reader = new JTokenReader(_json);
                _projectId = projectId;
            }

            public Scan Current => _currentScan;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public IEnumerator<Scan> GetEnumerator()
            {
                return new ScansReader(_json, _projectId);
            }

           Scan _currentScan = new Scan();

            public bool MoveNext()
            {
                while (JsonUtils.MoveToNextProperty(_reader))
                {
                    if (((JProperty)_reader.CurrentToken).Name.CompareTo("id") == 0)
                    {
                        _currentScan = new Scan()
                        {
                            ProjectId = _projectId,
                            ScanId = ((JProperty)_reader.CurrentToken).Value.ToString()
                        };

                        if (!JsonUtils.MoveToNextProperty(_reader, "startAnalyzeTime"))
                            return false;

                        _currentScan.StartTime = DateTime.Parse(((JProperty)_reader.CurrentToken).Value.ToString());

                        if (!JsonUtils.MoveToNextProperty(_reader, "endAnalyzeTime"))
                            return false;

                        _currentScan.FinishTime = DateTime.Parse(((JProperty)_reader.CurrentToken).Value.ToString());

                        if (!JsonUtils.MoveToNextProperty(_reader, "state"))
                            return false;

                        if (!JsonUtils.MoveToNextProperty(_reader, "name"))
                            return false;

                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ScansReader(_json, _projectId);
            }

        }


        public static IEnumerable<Scan> GetScans(CxRestContext ctx, CancellationToken token, 
            int projectId)
        {
            String url = CxRestContext.MakeUrl(ctx.Url, URL_SUFFIX, new Dictionary<String, String>()
            {
                {"projectId", Convert.ToString (projectId)  }
            });

            var scans = ctx.Json.CreateSastClient().GetAsync(url, token).Result;

            if (token.IsCancellationRequested)
                return null;

            if (!scans.IsSuccessStatusCode)
                throw new InvalidOperationException(scans.ReasonPhrase);


            JToken jt = JToken.Load(new JsonTextReader(new StreamReader
                (scans.Content.ReadAsStreamAsync().Result)));

            return new ScansReader(jt, projectId);
        }


    }
}