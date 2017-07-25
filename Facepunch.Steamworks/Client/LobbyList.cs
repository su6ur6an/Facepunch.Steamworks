﻿using System;
using System.Collections.Generic;
using System.Text;
using SteamNative;

namespace Facepunch.Steamworks
{
    public partial class LobbyList : IDisposable
    {
        internal Client client;
        public List<Lobby> Lobbies = new List<Lobby>();

        internal LobbyList(Client client)
        {
            this.client = client;
        }

        public void Refresh ( Filter filter = null)
        {
            if(filter == null)
            {
                filter = new Filter();
                filter.StringFilters.Add("appid", client.AppId.ToString());
            }

            client.native.matchmaking.AddRequestLobbyListDistanceFilter((SteamNative.LobbyDistanceFilter)filter.DistanceFilter);

            if(filter.SlotsAvailable != null)
            {
                client.native.matchmaking.AddRequestLobbyListFilterSlotsAvailable((int)filter.SlotsAvailable);
            }

            if (filter.MaxResults != null)
            {
                client.native.matchmaking.AddRequestLobbyListResultCountFilter((int)filter.MaxResults);
            }

            foreach (KeyValuePair<string, string> fil in filter.StringFilters)
            {
                client.native.matchmaking.AddRequestLobbyListStringFilter(fil.Key, fil.Value, SteamNative.LobbyComparison.Equal);
            }
            foreach (KeyValuePair<string, int> fil in filter.NearFilters)
            {
                client.native.matchmaking.AddRequestLobbyListNearValueFilter(fil.Key, fil.Value);
            }
            foreach (KeyValuePair<string, KeyValuePair<Filter.Comparison, int>> fil in filter.NumericalFilters)
            {
                client.native.matchmaking.AddRequestLobbyListNumericalFilter(fil.Key, fil.Value.Value, (SteamNative.LobbyComparison)fil.Value.Key);
            }


            // this will never return lobbies that are full (via the actual api)
            client.native.matchmaking.RequestLobbyList(OnLobbyList);

        }

        void OnLobbyList(LobbyMatchList_t callback, bool error)
        {
            if (error) return;
            Lobbies.Clear();
            uint lobbiesMatching = callback.LobbiesMatching;
            // lobbies are returned in order of closeness to the user, so add them to the list in that order
            for (int i = 0; i < lobbiesMatching; i++)
            {
                ulong lobby = client.native.matchmaking.GetLobbyByIndex(i);
                Lobby newLobby = Lobby.FromSteam(client, lobby);
                if (newLobby.Name != "")
                {
                    Lobbies.Add(newLobby);
                }
                else
                {
                    //need to sub the lobbyid to the request func
                    //client.native.matchmaking.RequestLobbyData(lobby)
                }

            }

            if (OnLobbiesRefreshed != null) { OnLobbiesRefreshed(); }
        }

        
        public Action OnLobbiesRefreshed;

        public void Dispose()
        {
            client = null;
        }

        public class Filter
        {
            // Filters that match actual metadata keys exactly
            public Dictionary<string, string> StringFilters = new Dictionary<string, string>();
            // Filters that are of string key and int value for that key to be close to
            public Dictionary<string, int> NearFilters = new Dictionary<string, int>();
            //Filters that are of string key and int value, with a comparison filter to say how we should relate to the value
            public Dictionary<string, KeyValuePair<Comparison, int>> NumericalFilters = new Dictionary<string, KeyValuePair<Comparison, int>>();
            public Distance DistanceFilter = Distance.Worldwide;
            public int? SlotsAvailable { get; set; }
            public int? MaxResults { get; set; }

            public enum Distance : int
            {
                Close = SteamNative.LobbyDistanceFilter.Close,
                Default = SteamNative.LobbyDistanceFilter.Default,
                Far = SteamNative.LobbyDistanceFilter.Far,
                Worldwide = SteamNative.LobbyDistanceFilter.Worldwide
            }

            public enum Comparison : int
            {
                EqualToOrLessThan = SteamNative.LobbyComparison.EqualToOrLessThan,
                LessThan = SteamNative.LobbyComparison.LessThan,
                Equal = SteamNative.LobbyComparison.Equal,
                GreaterThan = SteamNative.LobbyComparison.GreaterThan,
                EqualToOrGreaterThan = SteamNative.LobbyComparison.EqualToOrGreaterThan,
                NotEqual = SteamNative.LobbyComparison.NotEqual
            }
        }
    }
}
