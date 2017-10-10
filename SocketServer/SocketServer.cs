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
    * References: https://www.youtube.com/channel/UCjEwVWVFkhMRFoSYWXIND6Q - Videos: C# Socket Programming - Multiple Clients, C# Async Sockets Part 1: Basics, C# Async Sockets Part 2: Robustness, C# Async Sockets Part 3: Lightweight Packaging.
    *
    * \section description_sec Description
    * 
    * This application was designed to demonstrate the use of sockets in IPC for assessment three part b of the dynamic data structures unit. A server will accept multiple clients, When a client connects they must first input their name and click send, this will initiate the connection.
    * Once connected a client can enter text and click to send, this will be received on the server end and will be output within the server textbox with the users name displayed. If there are more than one client connected then that text will be output to other clients with the correct name displaying.
    * Upon clicking the exit button a client will disconnect.
    * Please note - if you get a "cannot block a call on this socket while an earlier asynchronous call is in progress" error, or an SystemIO error when KeyDown enter, just restart the program, it seems to happen randomly.
    */

    /// <summary>
    ///   Class for server operations.
    /// </summary>
    public partial class SocketServer : Form
    {
        //declaring member variables
        private Socket mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //< Socket to be used for the server

        private Client client; //!< Declaring an empty instance of Client.
        private byte[] mReceivedData = new byte[1024]; //!< Byte array to store data received.
        byte[] mSendData = new byte[1024]; //!< Byte array to store data send.

        private List<Client> mClients = new List<Client>(); //!< List to store clients.
        private List<Socket> mClientSockets = new List<Socket>(); //!< List to store client sockets.

        public SocketServer()
        {
            InitializeComponent();
            SetupServer();
        }

        /** 
        * Function to set up server which will allow the connection of 2 clients on the port 955 and will take any local ip address.
        */
        void SetupServer()
        {
            try
            {
                mServerSocket.Bind(new IPEndPoint(IPAddress.Any, 955));
                mServerSocket.Listen(5);

                // Server will begin accepting connections asynchronously.
                mServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (SocketException) { }
        }

        /**
         * When the server receives a connect to callback from a client it accepts it and adds that socket to the list of sockets.
         */
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket;

            try
            {
                socket = mServerSocket.EndAccept(ar);
                mClientSockets.Add(socket);

                // The server will also begin receiving data sent from the client. The first piece of data sent is the name of the client, the name will only be sent upon connecting so this will only be called once per client.
                // The data received from the client will be store within the mData byte array and that will be passed to the receive callback.
                socket.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, new AsyncCallback(RecieveNameCallback), socket);

                // From then the only text data will be sent.
                socket.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, new AsyncCallback(RecieveCallback), socket);
                mServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch (SocketException) { }
        }

        /**
         * This method handles receiving name data from the client and printing that name, only called once upon a client connecting.
         */
        private void RecieveNameCallback(IAsyncResult ar)
        {
            var current = (Socket)ar.AsyncState;
            var received = 0;

            try
            {
                received = current.EndReceive(ar);

                if (received == 0)
                {
                    return;
                }

                // Creating a new client from the data received from the client, the client class has an overloaded constructor that takes a byte array and converts that data.
                client = new Client(mReceivedData);
                mClients.Add(client);

                var line = "";
                line = client.Name + " has entered the room";
                AppFunctions.AppendTextBox(line, receivedTxt);

                foreach (Socket client in mClientSockets)
                {
                    AppFunctions.SendText(line, client, mSendData);
                }

                // Ensure received data array is cleared for the next piece of data.
                Array.Clear(mReceivedData, 0, mReceivedData.Length);
            }
            catch (SocketException) { }
        }
    
        /**
         * Method for handling all text sent from a client.
         * */
        private void RecieveCallback(IAsyncResult ar)
        {
            var current = (Socket)ar.AsyncState;
            var received = 0;
            var currentIndex = 0;
            currentIndex = UpdateIndex(current);

            try
            {
                received = current.EndReceive(ar);

                if (received == 0)
                {
                    return;
                }

                // Parsing the data sent, need to trim the end of any null characters.
                var text = "";
                text = Encoding.ASCII.GetString(mReceivedData, mReceivedData[received], mReceivedData.Length).TrimEnd('\0');

                // Handles closing the socket when the client clicks the exit button.
                if (text == "/exit")
                {
                    var line = "";
                    line = mClients[currentIndex].Name + " has left the room";
                    AppFunctions.AppendTextBox(line, receivedTxt);

                    foreach (Socket client in mClientSockets)
                    {
                        AppFunctions.SendText(line, client, mSendData);
                    }

                    // Remove client from the client list and the curent socket from the socket list, clear the received data array so that other people can still send data.
                    mClientSockets.Remove(current);
                    mClients.Remove(mClients[currentIndex]);
                    Array.Clear(mReceivedData, 0, mReceivedData.Length);
                }
                else
                {
                    var line = "";
                    line = mClients[currentIndex].Name + " says: " + text;
                    AppFunctions.AppendTextBox(line, receivedTxt);

                    // Send the received data to all clients.
                    foreach (Socket client in mClientSockets)
                    {
                        AppFunctions.SendText(line, client, mSendData);
                    }

                    // Clear the data array for the next piece of incoming data.
                    Array.Clear(mReceivedData, 0, mReceivedData.Length);
                    current.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, new AsyncCallback(RecieveCallback), current);
                }
            }
            catch (SocketException) { }
        }

        /**
         *  Method to find the index of a current socket.
         *  @param socket   The current socket.
         */
        private int UpdateIndex(Socket socket)
        {
            int temp = 0;

            // Used to determine the index within the client list the current client is at.
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
