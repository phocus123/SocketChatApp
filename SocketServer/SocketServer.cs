using SocketClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace SocketServer
{
    /*! \mainpage Assessment Three Part B IPC Socket Application
     *
    * \section author_sec Author
    * Author: Mark Schafers
    * 
    * \section reference_sec References
    * References: https://www.youtube.com/channel/UCjEwVWVFkhMRFoSYWXIND6Q - Videos: C# Socket Programming - Multiple Clients, C# Async Sockets Part 1: Basics, C# Async Sockets Part 2: Robustness, C# Async Sockets Part 3: Lightweight Packaging
    *
    * \section description_sec Description
    * 
    * This application was designed to demonstrate the use of sockets in IPC for assessment three part b of the dynamic data structures unit. A server will accept multiple clients, When a client connects they must first input their name and click send, this will initiate the connection.
    * Once connected a client can enter text and click to send, this will be received on the server end and will be output within the server textbox with the users name displayed. If there are more than one client connected then that text will be output to other clients with the correct name displaying.
    * Upon clicking the exit button a client will disconnect
    * Please note - if you get a "cannot block a call on this socket while an earlier asynchronous call is in progress" error, or an SystemIO error when KeyDown enter, just restart the program, it seems to happen randomly 
    */

    /// <summary>
    ///   Class for server operations
    /// </summary>
    public partial class SocketServer : Form
    {
        //declaring member variables
        private Socket mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //< Socket to be used for the server

        private Client client; //!< declaring an empty instance of Client
        private byte[] mReceivedData = new byte[1024]; //!< byte array to store data received
        byte[] mSendData = new byte[1024]; //!< byte array to store data send

        private List<Client> mClients = new List<Client>(); //!< list to store clients
        private List<Socket> mClientSockets = new List<Socket>(); //!< list to store client sockets

        public SocketServer()
        {
            InitializeComponent();
            SetupServer();
        }

        /** 
        * function to set up server which will allow the connection of 2 clients on the port 955 and will take any local ip address
        */
        void SetupServer()
        {
            try
            {
                mServerSocket.Bind(new IPEndPoint(IPAddress.Any, 955));
                mServerSocket.Listen(5);

                //Server will begin accepting connections asynchronously
                mServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (SocketException) { }
        }

        /**
         * when the server receives a connect to callback from a client it accepts it and adds that socket to the list of sockets
         */
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket;

            try
            {
                socket = mServerSocket.EndAccept(ar);
                mClientSockets.Add(socket);

                //the server will also begin receiving data sent from the client. The first piece of data sent is the name of the client, the name will only be sent upon connecting so this will only be called once per client
                //the data received from the client will be store within the mData byte array and that will be passed to the receive callback
                socket.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, new AsyncCallback(RecieveNameCallback), socket);

                //from then the only text data will be sent
                socket.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, new AsyncCallback(RecieveCallback), socket);

                //because you are calling this method again at the end it will continously run over and over again?
                mServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (SocketException) { }
        }

        /**
         * this method handles receiving name data from the client and printing that name, only called once upon a client connecting      
         */
        private void RecieveNameCallback(IAsyncResult ar)
        {
            Socket current = (Socket)ar.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(ar);

                if (received == 0)
                {
                    return;
                }

                //creating a new client from the data received from the client, the client class has an overloaded constructor that takes a byte array and converts that data
                client = new Client(mReceivedData);
                mClients.Add(client);

                string line = client.Name + " has entered the room";
                AppFunctions.AppendTextBox(line, receivedTxt);

                foreach (Socket client in mClientSockets)
                {
                    //SendText(line, client);
                    AppFunctions.SendText(line, client, mSendData);
                }

                //ensure received data array is cleared for the next piece of data
                Array.Clear(mReceivedData, 0, mReceivedData.Length);
            }
            catch (SocketException) { }
        }
    
        /**
         * method for handling all text sent from a client
         * */
        private void RecieveCallback(IAsyncResult ar)
        {
            Socket current = (Socket)ar.AsyncState;
            int received;
            int currentIndex = UpdateIndex(current);

            try
            {
                received = current.EndReceive(ar);

                if (received == 0)
                {
                    return;
                }

                //parsing the data sent, need to trim the end of any null characters
                string text = Encoding.ASCII.GetString(mReceivedData, mReceivedData[received], mReceivedData.Length).TrimEnd('\0');

                //handles closing the socket when the client clicks the exit button
                if (text == "/exit")
                {
                    string line = mClients[currentIndex].Name + " has left the room";
                    AppFunctions.AppendTextBox(line, receivedTxt);

                    foreach (Socket client in mClientSockets)
                    {
                        AppFunctions.SendText(line, client, mSendData);
                    }

                    //remove client from the client list and the curent socket from the socket list, clear the received data array so that other people can still send data
                    mClientSockets.Remove(current);
                    mClients.Remove(mClients[currentIndex]);
                    Array.Clear(mReceivedData, 0, mReceivedData.Length);
                }
                else
                {
                    string line = mClients[currentIndex].Name + " says: " + text;
                    AppFunctions.AppendTextBox(line, receivedTxt);

                    // send the received data to all clients
                    foreach (Socket client in mClientSockets)
                    {
                        AppFunctions.SendText(line, client, mSendData);
                    }

                    //clear the data array for the next piece of incoming data
                    Array.Clear(mReceivedData, 0, mReceivedData.Length);
                    current.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, new AsyncCallback(RecieveCallback), current);
                }
            }
            catch (SocketException) { }
        }

        /**
         *  method to find the index of a current socket
         *  @param socket   the current socket
         */
        private int UpdateIndex(Socket socket)
        {
            int temp = 0;

            //used to determine the index within the client list the current client is at
            for (int i = 0; i < mClientSockets.Count; i++)  
            {
                if (socket == mClientSockets[i])
                {
                    temp = i;
                }
            }

            return temp;
        }
    }
}
