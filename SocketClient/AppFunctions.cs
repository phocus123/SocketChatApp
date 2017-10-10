using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;


namespace SocketClient
{
    /// <summary>
    ///   Static class for methods that can be used in both SocketClient and SocketServer classes.
    /// </summary>
    public static class AppFunctions 
    {

        /**
        * Method for updating a textbox.
        */
        public static void AppendTextBox(string text, TextBox txtBox)
        {
            MethodInvoker invoker = new MethodInvoker(delegate
            {
                txtBox.Text += text;
                txtBox.AppendText("\r\n" + "\r\n");
            });

            txtBox.Invoke(invoker);
        }

        /**
        * method for sending data to the server, this one takes a string argument for sending the text.
        * @param Text  the text to be sent.
        * @param socket    the client that sent it.
        * @param array  Array to hold the data.
        */
        public static void SendText(string text, Socket socket, byte[] array)
        {
            array = Encoding.ASCII.GetBytes(text);
            socket.Send(array, 0, array.Length, SocketFlags.None);
            Array.Clear(array, 0, array.Length);
        }
    }
}
