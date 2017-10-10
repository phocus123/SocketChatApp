using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace SocketClient
{
    /// <summary>
    ///   Class for client operations.
    /// </summary>
    public partial class SocketClient : Form
    {
        // Declaring member variables.
        private Socket mClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //< Socket to be used for the client
        private Client client; //!< declaring an empty instance of Client
        private byte[] mReceivedData = new byte[1024]; //!< byte array to store data received
        private byte[] mSendData = new byte[1024]; //!< byte array to store data send

        public SocketClient()
        {
            InitializeComponent();
        }

        /** 
        * Function to handle connecting to the server and ensuring a name is entered when the connect button is clicked.
        */
        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            try
            {
                // Ensure there is a name input.
                if (nameTxt.Text != string.Empty)
                {
                    // Connect using local ip address (ie. 127.0.0.1) and port 955.
                    mClientSocket.BeginConnect(new IPEndPoint(IPAddress.Loopback, 955), new AsyncCallback(ConnectToCallback), null);
                    SendName();
                }
                else
                {
                    MessageBox.Show("Please enter a name!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (SocketException) { }
        }

        /**
         * Callback function for connecting to a server.
         */
        private void ConnectToCallback(IAsyncResult ar)
        {
            try
            {
                mClientSocket.EndConnect(ar);
                UpdateControlStates(true);

                // Begin receiving data from the server.
                mClientSocket.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (SocketException) { }
        }

        /**
         * Function for receiving data from the server and displaying the data received in textbox.
         */
        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                int received = mClientSocket.EndReceive(AR);

                if (received == 0)
                {
                    return;
                }

                var message = Encoding.ASCII.GetString(mReceivedData).TrimEnd('\0');
                AppFunctions.AppendTextBox(message, receivedTxt);
                mClientSocket.BeginReceive(mReceivedData, 0, mReceivedData.Length, SocketFlags.None, ReceiveCallback, null);
                Array.Clear(mReceivedData, 0, mReceivedData.Length);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        /**
         * Function for sending the clients name upon connection. will only be called once.
         */
        private void SendName()
        {

            client = new Client(nameTxt.Text);
            SendText(client);
            nameTxt.Enabled = false;
        }

        /**
         * Function for toggling controls.
         */
        private void UpdateControlStates(bool toggle)
        {
            var invoker = new MethodInvoker(delegate
            {
                sendBtn.Enabled = toggle;
                exitBtn.Enabled = toggle;
                sendTxt.Enabled = toggle;
                nameTxt.Enabled = !toggle;
                connectBtn.Enabled = !toggle;
            });

            Invoke(invoker);
        }

        /** 
        * Function to handle sending data to the server when the send button is clicked.
        */
        private void SendBtn_Click(object sender, EventArgs e)
        {
            try
            {
                AppFunctions.SendText(sendTxt.Text, mClientSocket, mSendData);
                sendTxt.Clear();
            }
            catch (Exception)
            {
                UpdateControlStates(false);
            }
        }

        /**
         * Function for sending client object to the server.
         */
        private void SendText(Client client)
        {
            byte[] nameBuffer = client.ToByteArray();
            mClientSocket.Send(nameBuffer, 0, nameBuffer.Length, SocketFlags.None);
        }

        /**
         * Function to handle informing the server of a client disconnecting, closing the socket and quitting the application.
         */
        private void ExitBtn_Click(object sender, EventArgs e)
        {
            AppFunctions.SendText("/exit", mClientSocket, mSendData);
            mClientSocket.Shutdown(SocketShutdown.Both);
            mClientSocket.Close();
            Application.Exit();
        }

        /**
         * Function for detecting when the enter key is pressed whilst focused on the sendTxt textbox and sending the data.
         */
        private void SendTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
            {
                AppFunctions.SendText(sendTxt.Text, mClientSocket, mSendData);
                sendTxt.Clear();
                e.SuppressKeyPress = true;
            }
        }

        /**
        * Function for detecting when the enter key is pressed whilst focused on the nameTxt textbox and sending the data.
        */
        private void NameTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Enter))
            {
                if (nameTxt.Text != string.Empty)
                {
                    // Connect using local ip address (ie. 127.0.0.1) and port 955.
                    mClientSocket.BeginConnect(new IPEndPoint(IPAddress.Loopback, 955), new AsyncCallback(ConnectToCallback), null);
                    SendName();
                    e.SuppressKeyPress = true;
                }
            }
        }
    }
}

