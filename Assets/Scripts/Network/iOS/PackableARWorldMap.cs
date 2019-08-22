﻿using System;
using Unity.Collections;
using UnityEngine.XR.ARKit;

namespace ARPeerToPeerSample.Network
{
    public struct PackableARWorldMap
    {
        public readonly byte[] ARWorldMapData;

        public PackableARWorldMap(byte[] arWorldMapData)
        {
            ARWorldMapData = arWorldMapData;
        }

        public static explicit operator ARWorldMap(PackableARWorldMap arWorldMap)
        {
            using (var data = new NativeArray<byte>(arWorldMap.ARWorldMapData.Length, Allocator.Temp))
            {
                data.CopyFrom(arWorldMap.ARWorldMapData);
                if (!ARWorldMap.TryDeserialize(data, out var worldMap))
                {
                    throw new Exception("ARWorldMap Deserialize was unsuccessful");
                }

                if (!worldMap.valid)
                {
                    throw new Exception("Data is not a valid ARWorldMap");
                }

                return worldMap;
            }
        }

        public static explicit operator PackableARWorldMap(ARWorldMap arWorldMap)
        {
            return new PackableARWorldMap(arWorldMap.Serialize(Allocator.Temp).ToArray());
        }
    }
}