using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

public class PipeServer
{
    private static int numThreads = 4;
    public static void Main()
    {
        int i;
        Thread[] servers = new Thread[numThreads];
        Console.WriteLine("\n*** Named pipe server stream with impersonation example ***\n");
        Console.WriteLine("Waiting for client connect...\n");
        for (i = 0; i < numThreads; i++)
        {
            servers[i] = new Thread(ServerThread);
            servers[i].Start();
        }
        Thread.Sleep(250);
        for(int j = 0;j < numThreads; j++){
            if (servers[j] != null)
            {
                if (servers[j].Join(250))
                {
                    Console.WriteLine("Server thread[{0}] finished.", servers[j].ManagedThreadId);
                    servers[j] = null;
                    i--;
                }
            }
        }
        Console.WriteLine("\nServer threads exhausted exiting.");
    }
    private static void ServerThread(object data)
    {
        NamedPipeServerStream pipeServer =
            new NamedPipeServerStream("testpipe", PipeDirection.InOut, numThreads);
        int threadId = Thread.CurrentThread.ManagedThreadId;

        //wait for client to connect
        pipeServer.WaitForConnection();

        Console.WriteLine("Client connected on thread[{0}].", threadId);
        try
        {
            //Read the request from the client. Once the client has
            //written to the pipe its security token will be available.
            //We might not need this security token ? 
            //We need interprocess communication on the same computer
            StreamString ss = new StreamString(pipeServer);

            //Verify our identity to the connected client using a 
            //string that the client anticipates

            ss.WriteString("I am the one true server!");
            string filename = ss.ReadString();

            //Read in the contents of the file while impersonating the client.
            ReadFileToStream fileReader = new ReadFileToStream(ss, filename);

            //Display the name of the user we are impersonating. //Impersonating ? 
            Console.WriteLine("Reading file: {0} on thread{1} as user: {2}.",
                filename, threadId, pipeServer.GetImpersonationUserName()); //So impersonation is nothing but name of client at the other end ? Cool!
            pipeServer.RunAsClient(fileReader.Start);
        }
        catch(IOException e)
        {
            Console.WriteLine("ERROR: {0}", e.Message);
        }
        pipeServer.Close();
    }
}

public class StreamString
{
    private Stream ioStream;
    private UnicodeEncoding streamEncoding;

    public StreamString(Stream ioStream)
    {
        this.ioStream = ioStream;
        streamEncoding = new UnicodeEncoding();
    }
    public string ReadString()
    {
        int len = 0;
        len = ioStream.ReadByte() * 256; //?
        len += ioStream.ReadByte();
        byte[] inBuffer = new byte[len];
        ioStream.Read(inBuffer, 0, len);
        return streamEncoding.GetString(inBuffer);
    }
    public int WriteString(string outString)
    {
        byte[] outBuffer = streamEncoding.GetBytes(outString);
        int len = outBuffer.Length;
        if(len > UInt16.MaxValue)
        {
            len = (int)UInt16.MaxValue;
        }
        ioStream.WriteByte((Byte)(len / 256));
        ioStream.WriteByte((Byte)(len & 255));
        ioStream.Write(outBuffer, 0, len);
        ioStream.Flush();
        return outBuffer.Length + 2;
    }

}

public class ReadFileToStream
{
    private string fn;
    private StreamString ss;

    public ReadFileToStream(StreamString str, string filename)
    {
        fn = filename;
        ss = str;
    }
    public void Start() //all the functions start with a Capital letter Fak
    {
        string contents = File.ReadAllText(fn);
        ss.WriteString(contents); //Who the client is ? Part of Auth ? Methinks so
    }
}



