using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace AzureBlobCacheControlAdjust
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Count() != 4)
            {
                Console.WriteLine("usage: AzureBlobCacheControlAdjust.exe accountname key container maxage_in_seconds");
                Console.WriteLine("<key>");
                Console.ReadKey();
                return;
            }
            string accountname = args[0];
            string key = args[1];
            string container = args[2];
            string maxage = args[3];

            var credentials = new StorageCredentialsAccountAndKey(accountname, key);
            var baseAddress = String.Format("http://{0}.blob.core.windows.net/", accountname);
            var client = new CloudBlobClient(baseAddress, credentials);
            var cloudBlobContainer = client.GetContainerReference(baseAddress+container);

            var cacheControlHeader = String.Format("public, max-age={0}", maxage);

            // get the info for every blob in the container
            var blobInfos = cloudBlobContainer.ListBlobs(new BlobRequestOptions() {
                UseFlatBlobListing = true, 
                BlobListingDetails = BlobListingDetails.All
            }).Cast<CloudBlob>();

            Parallel.ForEach(blobInfos, new ParallelOptions() {MaxDegreeOfParallelism = Environment.ProcessorCount*2}, (blob) =>
                {
                    var needsSetProperties = false;
                    // get the blob properties
                    //CloudBlob blob = cloudBlobContainer.GetBlobReference(blobInfo.Uri.ToString());
                    blob.FetchAttributes();

                    // set cache-control header if necessary
                    if (blob.Properties.CacheControl != cacheControlHeader)
                    {
                        blob.Properties.CacheControl = cacheControlHeader;
                        needsSetProperties = true;
                        Console.Write("#");
                    }
                    else
                    {
                        Console.Write(".");
                    }

                    if (blob.Uri.AbsolutePath.ToLowerInvariant().EndsWith(".png") &&
                        blob.Metadata["optimized"] != "true")
                    {
                        Console.Write("P");

                        //download and test lossless compression
                        var f = Guid.NewGuid();
                        var inFilename = string.Format("{0}_IN.png", f);
                        var outFilename = string.Format("{0}_OUT.png", f);

                        blob.DownloadToFile(inFilename);

                        //execute pngout
                        var process = Process.Start(new ProcessStartInfo("pngout.exe", string.Format("{0} {1}", inFilename, outFilename)){CreateNoWindow = true,UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden});
                      
                        process.PriorityClass = ProcessPriorityClass.BelowNormal;
                        process.WaitForExit();

                        var originalLength = new FileInfo(inFilename).Length;
                        var length = new FileInfo(outFilename).Length;

                        blob.Metadata["optimized"] = "true";
                        needsSetProperties = true;

                        if (originalLength > length)
                        {
                            //we could save!
                            blob.Properties.CacheControl = cacheControlHeader;
                            blob.UploadFile(outFilename);
                        }

                    }

                    if (!needsSetProperties) return;
                    blob.SetProperties();
                    blob.SetMetadata();
                });

            Console.WriteLine();
            Console.WriteLine("Fertig.");
            Console.ReadKey();
        }
    }
}
