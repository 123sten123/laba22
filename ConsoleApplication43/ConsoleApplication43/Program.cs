using System;
using System.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using Newtonsoft.Json;
using System.Net;

public class HttpClient
{
    public bool Ping(string host, int port)
    {
        HttpStatusCode status;
        Request("GET", host, port, "Ping", out status);
        if (status == HttpStatusCode.OK)
        {
            return true;
        }
        return false;
    }

    public byte[] GetInputData(string host, int port)
    {
        HttpStatusCode status;
        return Request("GET", host, port, "GetInputData", out status);
    }

    public void WriteAnswer(string host, int port, byte[] outData)
    {
        HttpStatusCode status;
        Request("POST", host, port, "WriteAnswer", out status, outData);
    }

    private byte[] Request(string type, string host, int port, string method, out HttpStatusCode status, byte[] data = null)
    {
        UriBuilder builder = new UriBuilder();
        builder.Host = host;
        builder.Port = port;
        builder.Path = method;

        var request = WebRequest.CreateHttp(builder.Uri);
        request.Timeout = 1000;
        request.Method = type;

        if (data != null)
        {
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(data, 0, data.Length);
            }
        }

        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        status = response.StatusCode;
        using (var responseStream = response.GetResponseStream())
        {
            if (responseStream.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    responseStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        return null;
    }
}

public class Output
{
    public decimal SumResult { get; set; }
    public int MulResult { get; set; }
    public decimal[] SortedInputs { get; set; }
}
public class Input
{
    public int K { get; set; }
    public decimal[] Sums { get; set; }
    public int[] Muls { get; set; }
}

class Program
{
    static public Output CreateOutput(Input input)
    {
        var output = new Output();
        output.SumResult = input.Sums.Sum() * input.K;
        output.MulResult = input.Muls.Aggregate((p, x) => p *= x);

        var tmp = input.Sums.ToList();

        for (int i = 0; i < input.Muls.Length; i++)
        {
            tmp.Add(Convert.ToDecimal(input.Muls[i]));
        }

        tmp.Sort();
        output.SortedInputs = tmp.ToArray();

        return output;
    }

    static void Main(string[] args)
    {
        var newInput = new Input();
        var newOutput = new Output();

        int t = int.Parse(Console.ReadLine());
        HttpClient Client = new HttpClient();

        if (Client.Ping("127.0.0.1", t))
        {
            String input = System.Text.Encoding.UTF8.GetString(Client.GetInputData("127.0.0.1", t));
            newInput = JsonConvert.DeserializeObject<Input>(input);

            newOutput = CreateOutput(newInput);
            var outputString = JsonConvert.SerializeObject(newOutput);
            String output = outputString.Replace(Environment.NewLine, "").Replace(" ", "");
            Client.WriteAnswer("127.0.0.1", t, System.Text.Encoding.UTF8.GetBytes(output));
        }
        Console.ReadKey();
    }
}