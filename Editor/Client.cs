﻿using System.Collections.Generic;
using PythonTypes.Types.Collections;
using PythonTypes.Types.Network;
using PythonTypes.Types.Primitives;

namespace Editor
{
    public class Client
    {
        public bool IncludePackets { get; set; } = true;
        public int ClientIndex { get; set; } = 0;
        public string Address { get; set; } = "";
        public string Username { get; set; } = "";
        public Dictionary<long, string> CallIDList = new Dictionary<long, string>();
        public Dictionary<long, string> ServiceList = new Dictionary<long, string>();
        public Dictionary<string, string> BoundServices = new Dictionary<string, string>();
        
        public Client(int index)
        {
            // store the window reference
            this.ClientIndex = index;
        }

        protected void HandlePacket(PacketEntry entry)
        {
            int callID = 0;
            string call = "";
            
            if (entry.Packet.Type == PyPacket.PacketType.CALL_REQ)
            {
                // get callID
                if (entry.Packet.Source is PyAddressNode node)
                    callID = node.CallID;
                else if (entry.Packet.Source is PyAddressClient client)
                    callID = client.CallID;

                this.CallIDList[callID] = entry.Call;
                call = entry.Call;
                this.ServiceList[callID] = entry.Service;
            }

            if (entry.Packet.Type == PyPacket.PacketType.CALL_RSP)
            {
                if (entry.Packet.Destination is PyAddressNode node)
                    callID = node.CallID;
                else if (entry.Packet.Destination is PyAddressClient client)
                    callID = client.CallID;

                // payload must be a single substream for these handlers to do anything
                if (entry.Packet.Payload[0] is PySubStream == false)
                    return;

                PySubStream subStream = entry.Packet.Payload[0] as PySubStream;
                
                call = this.CallIDList[callID];

                if (call.EndsWith(" (MachoBindObject)") == true || call == "MachoBindObject")
                {
                    // store the information for resolving this bound
                    PyString id =
                        ((((subStream.Stream as PyTuple)[0] as PySubStruct)
                            .Definition as PySubStream).Stream as PyTuple)[0] as PyString;

                    this.BoundServices.Add(id, entry.Service + " (bound)");
                }
                
                // special case, GetInventory and GetInventoryFromId
                if (call == "GetInventory" || call == "GetInventoryFromId")
                {
                    // store the information for resolving this bound
                    PyString id =
                        (((subStream.Stream as PySubStruct).Definition as PySubStream)
                            .Stream as PyTuple)[0] as PyString;

                    this.BoundServices.Add(id, "BoundInventory (bound)");
                }
                
                // extra special case, SparseRowset are similar to bound services
                if (subStream.Stream is PyObjectData == true)
                {
                    PyObjectData objectData = subStream.Stream as PyObjectData;

                    if (objectData.Name == "util.SparseRowset")
                    {
                        // get the data off it
                        PyString id = ((((objectData.Arguments as PyTuple)[1] as PySubStruct).Definition as PySubStream)
                            .Stream as PyTuple)[0] as PyString;
                        
                        this.BoundServices.Add(id, "SparseRowset (bound) " + id.Value);
                    }
                }
            }
        }
    }
}