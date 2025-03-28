﻿// Copyright 2024 MAES
// 
// This file is part of MAES
// 
// MAES is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.
// 
// MAES is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
// Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with MAES. If not, see http://www.gnu.org/licenses/.
// 
// Contributors: Rasmus Borrisholt Schmidt, Andreas Sebastian Sørensen, Thor Beregaard
// 
// Original repository: https://github.com/Molitany/MAES

using System;
using System.Collections.Generic;

using Maes.Map;

namespace Maes.Algorithms.Exploration.Minotaur
{
    public partial class MinotaurAlgorithm : IExplorationAlgorithm
    {
        public class DoorwayFoundMessage : IMinotaurMessage
        {
            private readonly Doorway _doorway;

            private readonly int _requesterID;

            public DoorwayFoundMessage(Doorway doorway, int requesterID)
            {
                _doorway = doorway;
                _requesterID = requesterID;
            }

            public IMinotaurMessage Combine(IMinotaurMessage otherMessage, MinotaurAlgorithm minotaur)
            {
                return this;
            }

            /// <summary>
            /// Process DoorwayFoundMessage, bidding on entering the doorway depending on the length of its path to it
            /// returns null it has to pass doorways to do so
            /// </summary>
            public IMinotaurMessage? Process(MinotaurAlgorithm minotaur)
            {
                var doorwayTile = CoarseGrainedMap.FromSlamMapCoordinate(_doorway.Center);
                var pathLengthToDoorway = minotaur._map.GetPath(doorwayTile, beOptimistic: true);
                if (pathLengthToDoorway != null)
                {
                    foreach (var knownDoorway in minotaur._doorways)
                    {
                        if (Array.IndexOf(pathLengthToDoorway, CoarseGrainedMap.FromSlamMapCoordinate(knownDoorway.Center)) != -1)
                        {
                            return null;
                        }
                    }
                    var bid = new Dictionary<int, int>() { { minotaur._controller.Id, pathLengthToDoorway.Length } };
                    minotaur._doorways.Add(_doorway);
                    return new BiddingMessage(_requesterID, bid, _doorway);
                }
                if (!minotaur._doorways.Contains(_doorway))
                {
                    minotaur._doorways.Add(_doorway);
                }

                return null;
            }
        }
    }
}