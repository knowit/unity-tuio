/*
 TUIO C# Library - part of the reacTIVision project
 Copyright (c) 2005-2014 Martin Kaltenbrunner <martin@tuio.org>

 This library is free software; you can redistribute it and/or
 modify it under the terms of the GNU Lesser General Public
 License as published by the Free Software Foundation; either
 version 3.0 of the License, or (at your option) any later version.
 
 This library is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 Lesser General Public License for more details.
 
 You should have received a copy of the GNU Lesser General Public
 License along with this library.
*/

using System.Collections.Generic;

using OSC.NET;

namespace TUIO
{
    /**
     * <remarks>
     * The TuioClient class is the central TUIO protocol decoder component. It provides a simple callback infrastructure using the {@link TuioListener} interface.
	 * In order to receive and decode TUIO messages an instance of TuioClient needs to be created. The TuioClient instance then generates TUIO events
	 * which are broadcasted to all registered classes that implement the {@link TuioListener} interface.
     * </remarks>
     * <example>
     * <code>
     * TuioClient client = new TuioClient();
	 * client.addTuioListener(myTuioListener);
	 * client.start();
     * </code>
     * </example>
     * 
     * @author Martin Kaltenbrunner
     * @version 1.1,5
     */
    public class TuioClient
    {
        private object objectSync = new object();

        private Dictionary<long, TuioObject> objectList = new Dictionary<long, TuioObject>(32);
        private List<long> aliveObjectList = new List<long>(32);
        private List<long> newObjectList = new List<long>(32);
        private List<TuioObject> frameObjects = new List<TuioObject>(32);

        private int currentFrame = 0;
        private TuioTime currentTime;


        /**
         * <summary>
		 * The default constructor creates a client that listens to the default TUIO port 3333</summary>
		 */
        public TuioClient() { }



        /**
		 * <summary>
         * The OSC callback method where all TUIO messages are received and decoded
		 * and where the TUIO event callbacks are dispatched</summary>
         * <param name="message">the received OSC message</param>
		 */
        public void ProcessMessage(OSCMessage message)
        {
            string address = message.Address;
            var args = message.Values;
            string command = (string)args[0];

            if (address == "/tuio/2Dobj")
            {
                if (command == "set")
                {

                    long s_id = (int)args[1];
                    int f_id = (int)args[2];
                    float xpos = (float)args[3];
                    float ypos = (float)args[4];
                    float angle = (float)args[5];
                    float xspeed = (float)args[6];
                    float yspeed = (float)args[7];
                    float rspeed = (float)args[8];
                    float maccel = (float)args[9];
                    float raccel = (float)args[10];

                    lock (objectSync)
                    {
                        if (!objectList.ContainsKey(s_id))
                        {
                            TuioObject addObject = new TuioObject(s_id, f_id, xpos, ypos, angle);
                            frameObjects.Add(addObject);
                        }
                        else
                        {
                            TuioObject tobj = objectList[s_id];
                            if (tobj == null) return;
                            if ((tobj.X != xpos) || (tobj.Y != ypos) || (tobj.Angle != angle) || (tobj.XSpeed != xspeed) || (tobj.YSpeed != yspeed) || (tobj.RotationSpeed != rspeed) || (tobj.MotionAccel != maccel) || (tobj.RotationAccel != raccel))
                            {

                                TuioObject updateObject = new TuioObject(s_id, f_id, xpos, ypos, angle);
                                updateObject.update(xpos, ypos, angle, xspeed, yspeed, rspeed, maccel, raccel);
                                frameObjects.Add(updateObject);
                            }
                        }
                    }

                }
                else if (command == "alive")
                {

                    newObjectList.Clear();
                    for (int i = 1; i < args.Count; i++)
                    {
                        // get the message content
                        long s_id = (int)args[i];
                        newObjectList.Add(s_id);
                        // reduce the object list to the lost objects
                        if (aliveObjectList.Contains(s_id))
                            aliveObjectList.Remove(s_id);
                    }

                    // remove the remaining objects
                    lock (objectSync)
                    {
                        for (int i = 0; i < aliveObjectList.Count; i++)
                        {
                            long s_id = aliveObjectList[i];
                            TuioObject removeObject = objectList[s_id];
                            removeObject.remove(currentTime);
                            frameObjects.Add(removeObject);
                        }
                    }

                }
                else if (command == "fseq")
                {
                    int fseq = (int)args[1];
                    bool lateFrame = false;

                    if (fseq > 0)
                    {
                        if (fseq > currentFrame) currentTime = TuioTime.SessionTime;
                        if ((fseq >= currentFrame) || ((currentFrame - fseq) > 100)) currentFrame = fseq;
                        else lateFrame = true;
                    }
                    else if ((TuioTime.SessionTime.TotalMilliseconds - currentTime.TotalMilliseconds) > 100)
                    {
                        currentTime = TuioTime.SessionTime;
                    }

                    if (!lateFrame)
                    {

                        IEnumerator<TuioObject> frameEnum = frameObjects.GetEnumerator();
                        while (frameEnum.MoveNext())
                        {
                            TuioObject tobj = frameEnum.Current;

                            switch (tobj.TuioState)
                            {
                                case TuioObject.TUIO_REMOVED:
                                    TuioObject removeObject = tobj;
                                    removeObject.remove(currentTime);

                                    lock (objectSync)
                                    {
                                        objectList.Remove(removeObject.SessionID);
                                    }
                                    break;
                                case TuioObject.TUIO_ADDED:
                                    TuioObject addObject = new TuioObject(currentTime, tobj.SessionID, tobj.SymbolID, tobj.X, tobj.Y, tobj.Angle);
                                    lock (objectSync)
                                    {
                                        objectList.Add(addObject.SessionID, addObject);
                                    }

                                    break;
                                default:
                                    TuioObject updateObject = getTuioObject(tobj.SessionID);
                                    if ((tobj.X != updateObject.X && tobj.XSpeed == 0) || (tobj.Y != updateObject.Y && tobj.YSpeed == 0))
                                        updateObject.update(currentTime, tobj.X, tobj.Y, tobj.Angle);
                                    else
                                        updateObject.update(currentTime, tobj.X, tobj.Y, tobj.Angle, tobj.XSpeed, tobj.YSpeed, tobj.RotationSpeed, tobj.MotionAccel, tobj.RotationAccel);

                                    break;
                            }
                        }


                        List<long> buffer = aliveObjectList;
                        aliveObjectList = newObjectList;
                        // recycling the List
                        newObjectList = buffer;
                    }
                    frameObjects.Clear();
                }

            }
            

			
        }

        #region Object Management

        /**
		 * <summary>
         * Returns a List of all currently active TuioObjects</summary>
         * <returns>a List of all currently active TuioObjects</returns>
		 */
        public List<TuioObject> getTuioObjects()
        {
            List<TuioObject> listBuffer;
            lock (objectSync)
            {
                listBuffer = new List<TuioObject>(objectList.Values);
            }
            return listBuffer;
        }

        /**
         * <summary>
         * Returns the TuioObject corresponding to the provided Session ID
         * or NULL if the Session ID does not refer to an active TuioObject</summary>
         * <returns>an active TuioObject corresponding to the provided Session ID or NULL</returns>
         */
        public TuioObject getTuioObject(long s_id)
        {
            TuioObject tobject = null;
            lock (objectSync)
            {
                objectList.TryGetValue(s_id, out tobject);
            }
            return tobject;
        }

        #endregion

    }
}
