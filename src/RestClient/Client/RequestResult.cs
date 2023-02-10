using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace RestClient.Client {
    public class RequestResult {
        public HttpRequestMessage? Request { get; internal set; }
        public HttpResponseMessage? Response { get; internal set; }
        public string? ErrorMessage { get; internal set; }
        public Request? RequestToken { get; internal set; }
        public string? ResponseTime { get; internal set; }

        public string? ResponseSize { get; internal set; }

        public string ResponseStatus => $"{(int)Response?.StatusCode} {Response?.ReasonPhrase}";

        public List<SimpleHeader>? Headers { get; internal set; }
    }
    public class SimpleHeader {
        public string? Name { get; set; }
        public string? Value { get; set; }

        public SimpleHeader(string name, string value) {
            Name = name;
            Value = value;
        }
    }
}
