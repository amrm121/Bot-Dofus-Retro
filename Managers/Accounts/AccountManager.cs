﻿using System;
using Bot_Dofus_1._29._1.Game.Mapas;
using Bot_Dofus_1._29._1.Game.Server;
using Bot_Dofus_1._29._1.Managers.Characters;
using Bot_Dofus_1._29._1.Managers.Fights;

namespace Bot_Dofus_1._29._1.Managers.Accounts
{
    public class AccountManager : IManager, IDisposable
    {
        public GameServer Server { get; private set; }
        public Map Map { get; private set; }
        public Character Character { get; private set; }
        public CharacterManager manager { get; private set; }
        public Pelea fight { get; private set; }
        private bool _disposed;

        internal AccountManager(Account prmAccount)
        {
            Server = new GameServer();
            Map = new Map();
            Character = new Character(prmAccount);
            manager = new CharacterManager(prmAccount, Map, Character);
            fight = new Pelea(prmAccount);
        }

        #region Zona Dispose
        ~AccountManager() => Dispose(false);
        public void Dispose() => Dispose(true);

        public void Clear()
        {
            Map.Clear();
            manager.Clear();
            fight.Clear();
            Character.Clear();
            Server.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Map.Dispose();
                    Character.Dispose();
                    manager.Dispose();
                    fight.Dispose();
                    Server.Dispose();
                }

                Server = null;
                Map = null;
                Character = null;
                manager = null;
                fight = null;
                _disposed = true;
            }
        }
        #endregion
    }
}
