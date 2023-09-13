using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IeeeVisUploaderWebApp.Tests.Helpers
{
    [TestClass()]
    public class StatisticsTests
    {
        [TestMethod()]
        public void RetrieveUploadRequestStatisticsTest()
        {
            const string folder = "logs";
            var files = new DirectoryInfo(folder).GetFiles("stdout_*.log");
            //      v-full-1531 file upload for video-ff with size 2272907
            var numUploads = 0;
            var totalSize = 0L;
            foreach (var f in files)
            {
                foreach (var l in File.ReadLines(f.FullName))
                {
                    var idx = l.IndexOf(" with size ");
                    if(idx  == -1)
                        continue;
                    var size = int.Parse(l.AsSpan(idx + 11));
                    totalSize += size;
                    numUploads++;
                }
            }

            Trace.WriteLine($"{numUploads} uploads, {totalSize/(1024d*1024*1024)} GB");
        }

        [TestMethod()]
        public void PerformChecksTest()
        {
            Assert.Fail();
        }
    }
}