#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"
#r "System.Web"
//  #load "../Helpers/copyBlobHelpers.csx"
//  #load "../Helpers/mediaServicesHelpers.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;
using Newtonsoft.Json;

// Read values from the App.config file.
private static readonly string _mediaServicesAccountName = Environment.GetEnvironmentVariable("AMSAccount");
private static readonly string _mediaServicesAccountKey = Environment.GetEnvironmentVariable("AMSKey");

static string _storageAccountName = Environment.GetEnvironmentVariable("MediaServicesStorageAccountName");
static string _storageAccountKey = Environment.GetEnvironmentVariable("MediaServicesStorageAccountKey");

private static CloudStorageAccount _destinationStorageAccount = null;

// Field for service context.
// private static CloudMediaContext _context = null;
// private static MediaServicesCredentials _cachedCredentials = null;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info($"Webhook was triggered! {req}");

    string jsonContent = await req.Content.ReadAsStringAsync();
    dynamic data = JsonConvert.DeserializeObject(jsonContent);
    string fileName = data.fileName;
    
    log.Info(fileName);
    
    if (string.IsNullOrEmpty(fileName))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, new
        {
            error = "Incorrect Path or File Name"
        });
    }

    log.Info($"C# Processing Blob trigger function for VTT file: {fileName}");

    try
    {
        var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));

        var blobClient = storageAccount.CreateCloudBlobClient();
        var container = blobClient.GetContainerReference("output-vtt");
        var blob = container.GetBlockBlobReference($"{fileName}");
        var contents = blob.OpenRead();
        
        log.Info($"Content: {contents}");
        
        Func<string, bool> isATimeMarker = text => Regex.IsMatch(text, @"^\d+");

        StringBuilder texto = new StringBuilder();

        using (StreamReader streamVTTFile = new StreamReader(contents))
        {
            string line = streamVTTFile.ReadLine();
            while ((line = streamVTTFile.ReadLine()) != null)
            {
                if (!isATimeMarker(line))
                {
                    //log.Info($"Line: {line}");
                    texto.Append(line);
                }
            }
        }
        
        //outputQueueVTT.Add(texto.ToString());

        log.Info($"Texto: {texto.ToString()}");

        return req.CreateResponse(HttpStatusCode.OK, new {Content = texto.ToString() });
    }
    catch (Exception ex)
    {
        log.Error("ERROR: failed.");
        log.Info($"StackTrace : {ex.StackTrace}");
        throw ex;
    }

}
