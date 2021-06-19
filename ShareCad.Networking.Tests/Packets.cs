using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShareCad.Networking.Packets;
using System;
using System.Windows;
using System.Xml;

namespace ShareCad.Networking.Tests
{
    [TestClass]
    public class Packets
    {
        [TestMethod]
        public void TestDocumentUpdateSerialization()
        {
            const string StringData = "<note><to>Tove</to><from>Jani</from><heading>Reminder</heading><body>Don't forget me this weekend!</body></note>";

            XmlDocument document = new XmlDocument();
            document.LoadXml(StringData);

            DocumentUpdate packet = new DocumentUpdate(document);
            byte[] data = packet.Serialize();

            Assert.AreEqual(StringData.Length, data.Length - sizeof(int));

            int specifiedLength = BitConverter.ToInt32(data, 0);
            Assert.AreEqual(StringData.Length, specifiedLength);
        }

        [TestMethod]
        public void TestCursorUpdateServerSerialization()
        {
            Point testPosition = new Point(10, 10);
            byte testCollaborator = 2;

            CursorUpdateServer packet = new CursorUpdateServer(testCollaborator, testPosition);
            byte[] data = packet.Serialize();


            Assert.AreEqual(testCollaborator, data[0]);

            /// TODO: Implement point.
        }
    }
}
