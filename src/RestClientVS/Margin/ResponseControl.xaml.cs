using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Controls;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using RestClient.Client;

namespace RestClientVS.Margin
{
    public partial class ResponseControl : UserControl
    {
        public ResponseControl()
        {
            InitializeComponent();
        }

        public async Task SetResponseTextAsync(RequestResult result)
        {
            if (result.Response != null)
            {
                // set the response time
                this.DataContext = result;
                
                
                var mediaType = result.Response.Content.Headers?.ContentType?.MediaType;
                if (mediaType == null)
                {
                    Control.Text = await result.Response.Content.ReadAsStringAsync();
                    return;
                }
                if (mediaType.IndexOf("json", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var jsonString = await result.Response.Content.ReadAsStringAsync();
                    var token = JsonObject.Parse(jsonString);
                    Control.Text = token.ToJsonString(new JsonSerializerOptions() { WriteIndented = true });
                    return;
                }
                if (mediaType.IndexOf("xml", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    var xmlString = await result.Response.Content.ReadAsStringAsync();
                    var xmlElement = XElement.Parse(xmlString);
                    Control.Text = xmlElement.ToString();
                    return;
                }
                Control.Text = await result.Response.ToRawStringAsync();
            }
            else
            {
                Control.Text = result.ErrorMessage;
            }
        }
    }
    
    
}
