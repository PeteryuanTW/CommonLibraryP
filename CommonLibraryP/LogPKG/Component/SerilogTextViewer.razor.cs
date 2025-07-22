using DevExpress.Blazor;
using DevExpress.Utils.Svg;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CommonLibraryP.LogPKG.Component
{
    public partial class SerilogTextViewer
    {
        private IReadOnlyList<IFileInputSelectedFile> files;

        private async Task SetFiles(FilesUploadingEventArgs args)
        {
            files = args.Files;
            await ReadSerilogData();
        }

        public async Task ReadSerilogData()
        {
            var res = new List<SerilogData>();
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        try
                        {
                            var record = JsonSerializer.Deserialize<SerilogData>(line);
                            if (record != null)
                                res.Add(record);
                        }
                        catch (JsonException ex)
                        {

                        }
                    }
                }
            }
            await serilogLogViewerBase?.SetLogDatas(res);
        }


    }
}
