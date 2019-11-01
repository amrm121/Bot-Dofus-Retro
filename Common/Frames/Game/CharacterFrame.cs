﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bot_Dofus_1._29._1.Common.Frames.Transport;
using Bot_Dofus_1._29._1.Common.Network;
using Bot_Dofus_1._29._1.Game.Character;
using Bot_Dofus_1._29._1.Game.Character.Jobs;
using Bot_Dofus_1._29._1.Game.Enums;
using Bot_Dofus_1._29._1.Game.Mapas.Entidades;
using Bot_Dofus_1._29._1.Managers;
using Bot_Dofus_1._29._1.Managers.Accounts;
using Bot_Dofus_1._29._1.Managers.Characters;

/*
    Este archivo es parte del proyecto BotDofus_1.29.1

    BotDofus_1.29.1 Copyright (C) 2019 Alvaro Prendes — Todos los derechos reservados.
    Creado por Alvaro Prendes
    web: http://www.salesprendes.com
*/

namespace Bot_Dofus_1._29._1.Common.Frames.Game
{
    class CharacterFrame : Frame
    {
        [PacketHandler("As")]
        public void GetStatsUpdate(TcpClient prmClient, string prmPacket) => prmClient.account.Game.Character.actualizar_Caracteristicas(prmPacket);

        [PacketHandler("PIK")]
        public void GetGroupRequest(TcpClient prmClient, string prmPacket)
        {
            prmClient.account.logger.log_informacion("Groupe", $"Nouvelle invitation de groupe du personnage: {prmPacket.Substring(3).Split('|')[0]}");
            prmClient.SendPacket("PR");
            prmClient.account.logger.log_informacion("Groupe", "Rejêt de l'invitation");
        }

        [PacketHandler("SL")]
        public void GetSpellList(TcpClient prmClient, string prmPacket)
        {
            if (!prmPacket[2].Equals('o'))
                prmClient.account.Game.Character.actualizar_Hechizos(prmPacket.Substring(2));
        }

        [PacketHandler("Ow")]
        public void GetPodsUpdate(TcpClient prmClient, string prmPacket)
        {
            string[] pods = prmPacket.Substring(2).Split('|');
            short currentPods = short.Parse(pods[0]);
            short maxPods = short.Parse(pods[1]);
            Character character = prmClient.account.Game.Character;

            character.inventario.pods_actuales = currentPods;
            character.inventario.pods_maximos = maxPods;
            prmClient.account.Game.Character.evento_Pods_Actualizados();
        }

        [PacketHandler("DV")]
        public void GetDialogClose(TcpClient prmClient, string prmPacket)
        {
            Account account = prmClient.account;

            switch (account.accountState)
            {
                case AccountState.STORAGE:
                    account.Game.Character.inventario.evento_Almacenamiento_Abierto();
                    break;

                case AccountState.DIALOG:
                    IEnumerable<Npcs> npcs = account.Game.Map.lista_npcs();
                    Npcs npc = npcs.ElementAt((account.Game.Character.hablando_npc_id * -1) - 1);
                    npc.respuestas.Clear();
                    npc.respuestas = null;
                    account.accountState = AccountState.CONNECTED_INACTIVE;
                    account.Game.Character.evento_Dialogo_Acabado();
                    break;
            }
        }

        [PacketHandler("EV")]
        public void GetWindowClosed(TcpClient prmClient, string prmPacket)
        {
            Account account = prmClient.account;

            if (account.accountState == AccountState.STORAGE)
            {
                account.accountState = AccountState.CONNECTED_INACTIVE;
                account.Game.Character.inventario.evento_Almacenamiento_Cerrado();
            }
        }

        [PacketHandler("JS")]
        public void GetProfessionsSkills(TcpClient prmClient, string prmPacket)
        {
            string[] skillMsgSplitted;
            Character character = prmClient.account.Game.Character;
            Job profession;
            JobSkills skill = null;
            short professionId, skillId;
            byte minQuantity, maxQuantity;
            float time;

            foreach (string professionData in prmPacket.Substring(3).Split('|'))
            {
                professionId = short.Parse(professionData.Split(';')[0]);
                profession = character.oficios.Find(x => x.id == professionId);

                if (profession == null)
                {
                    profession = new Job(professionId);
                    character.oficios.Add(profession);
                }

                foreach (string skillData in professionData.Split(';')[1].Split(','))
                {
                    skillMsgSplitted = skillData.Split('~');
                    skillId = short.Parse(skillMsgSplitted[0]);
                    minQuantity = byte.Parse(skillMsgSplitted[1]);
                    maxQuantity = byte.Parse(skillMsgSplitted[2]);
                    time = float.Parse(skillMsgSplitted[4]);
                    skill = profession.skills.Find(actividad => actividad.id == skillId);

                    if (skill != null)
                        skill.set_Actualizar(skillId, minQuantity, maxQuantity, time);
                    else
                        profession.skills.Add(new JobSkills(skillId, minQuantity, maxQuantity, time));
                }
            }

            character.evento_Oficios_Actualizados();
        }

        [PacketHandler("JX")]
        public void GetProfessionExperience(TcpClient prmClient, string prmPacket)
        {
            string[] professionExperienceSplitted = prmPacket.Substring(3).Split('|');
            Character character = prmClient.account.Game.Character;
            uint currentExperience, baseExperience, nextLevelExperience;
            short id;
            byte level;

            foreach (string profession in professionExperienceSplitted)
            {
                id = short.Parse(profession.Split(';')[0]);
                level = byte.Parse(profession.Split(';')[1]);
                baseExperience = uint.Parse(profession.Split(';')[2]);
                currentExperience = uint.Parse(profession.Split(';')[3]);

                if (level < 100)
                    nextLevelExperience = uint.Parse(profession.Split(';')[4]);
                else
                    nextLevelExperience = 0;

                character.oficios.Find(x => x.id == id).set_Actualizar_Oficio(level, baseExperience, currentExperience, nextLevelExperience);
            }
            character.evento_Oficios_Actualizados();
        }

        [PacketHandler("Re")]
        public void GetMontData(TcpClient prmClient, string prmPacket) => prmClient.account.canUseMount = true;

        [PacketHandler("OAKO")]
        public void GetObjectAppear(TcpClient prmClient, string prmPacket) => prmClient.account.Game.Character.inventario.agregar_Objetos(prmPacket.Substring(4));

        [PacketHandler("OR")]
        public void GetDeleteObject(TcpClient prmClient, string prmPacket) => prmClient.account.Game.Character.inventario.eliminar_Objeto(uint.Parse(prmPacket.Substring(2)), 1, false);

        [PacketHandler("OQ")]
        public void GetObjectQuantityModification(TcpClient prmClient, string prmPacket) => prmClient.account.Game.Character.inventario.modificar_Objetos(prmPacket.Substring(2));

        [PacketHandler("ECK")]
        public void GetOpenExchangeWindow(TcpClient prmClient, string prmPacket) => prmClient.account.accountState = AccountState.STORAGE;

        [PacketHandler("PCK")]
        public void GetGroupAccepted(TcpClient prmClient, string prmPacket) => prmClient.account.Game.Character.en_grupo = true;

        [PacketHandler("PV")]
        public void GetGroupLeave(TcpClient prmClient, string prmPacket) => prmClient.account.Game.Character.en_grupo = true; // TODO check this line

        [PacketHandler("ERK")]
        public void GetExchangeRequest(TcpClient prmClient, string prmPacket)
        {
            prmClient.account.logger.log_informacion("INFORMATION", "L'invitation à l'échange est rejetée");
            prmClient.SendPacket("EV", true);
        }

        [PacketHandler("ILS")]
        public void GetRegenerationTime(TcpClient prmClient, string prmPacket)
        {
            prmPacket = prmPacket.Substring(3);
            int time = int.Parse(prmPacket);
            Account account = prmClient.account;
            Character character = account.Game.Character;

            character.timer_regeneracion.Change(Timeout.Infinite, Timeout.Infinite);
            character.timer_regeneracion.Change(time, time);

            account.logger.log_informacion("DOFUS", $"Votre personnage récupère 1 pdv chaque {time / 1000} secondes");
        }

        [PacketHandler("ILF")]
        public void GetLifeQuantityRegenerated(TcpClient prmClient, string prmPacket)
        {
            prmPacket = prmPacket.Substring(3);
            int hp = int.Parse(prmPacket);
            Account account = prmClient.account;
            Character character = account.Game.Character;

            character.caracteristicas.vitalidad_actual += hp;
            account.logger.log_informacion("DOFUS", $"Vous avez récupéré {hp} points de vie");
        }

        [PacketHandler("eUK")]
        public void GetReceivedEmote(TcpClient prmClient, string prmPacket)
        {
            string[] splittedData = prmPacket.Substring(3).Split('|');
            int id = int.Parse(splittedData[0]), emote_id = int.Parse(splittedData[1]);
            Account account = prmClient.account;

            if (account.Game.Character.Id != id)
                return;

            if (emote_id == 1 && account.accountState != AccountState.REGENERATION)
                account.accountState = AccountState.REGENERATION;
            else if (emote_id == 0 && account.accountState == AccountState.REGENERATION)
                account.accountState = AccountState.CONNECTED_INACTIVE;
        }

        [PacketHandler("Bp")]
        public void GetPingAverage(TcpClient prmClient, string prmPacket) => prmClient.SendPacket($"Bp{prmClient.GetPingAverage()}|{prmClient.GetTotalPings()}|50");

        [PacketHandler("pong")]
        public void GetPingPong(TcpClient prmClient, string prmPacket) => prmClient.account.logger.log_informacion("DOFUS", $"Ping: {prmClient.GetPing()} ms");
    }
}
