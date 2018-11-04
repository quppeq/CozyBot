﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace DiscordBot1
{
    /// <summary>
    /// Abstract Class describes user content, which can be saved and posted by bot.
    /// </summary>
    public abstract class ContentModule : IBotModule
    {
        /// <summary>
        /// Configuration commands list.
        /// </summary>
        protected List<IBotCommand> _cfgCommands = new List<IBotCommand>();

        /// <summary>
        /// Content Use commands list.
        /// </summary>
        protected List<IBotCommand> _useCommands = new List<IBotCommand>();

        /// <summary>
        /// Content Addition commands list.
        /// </summary>
        protected List<IBotCommand> _addCommands = new List<IBotCommand>();

        /// <summary>
        /// Content deletion commands list.
        /// </summary>
        protected List<IBotCommand> _delCommands = new List<IBotCommand>();

        /// <summary>
        /// Boolean flag indicating if module is active.
        /// </summary>
        protected bool _isActive;

        /// <summary>
        /// Module's commands default prefix.
        /// </summary>
        protected static string _defaultPrefix = "c!";

        /// <summary>
        /// Module's commands current prefix.
        /// </summary>
        protected string _prefix;

        /// <summary>
        /// Bot's ID.
        /// </summary>
        protected ulong _clientId;

        /// <summary>
        /// List of guild admins IDs.
        /// </summary>
        protected List<ulong> _adminIds;

        /// <summary>
        /// XElement containing guild's modules config.
        /// </summary>
        protected XElement _configEl;

        /// <summary>
        /// XElement containing this module config.
        /// </summary>
        protected XElement _moduleConfigEl;

        /// <summary>
        /// Internal RNG.
        /// </summary>
        protected static Random _rnd = new Random();

        /// <summary>
        /// Internal configuration change event.
        /// </summary>
        protected event ConfigChanged _configChanged;

        /// <summary>
        /// Configuration Changed Event. Raised on changes to config.
        /// </summary>
        public event ConfigChanged ConfigChanged
        {
            add
            {
                if (value != null)
                {
                    _configChanged += value;
                }
            }
            remove
            {
                if (value != null)
                {
                    _configChanged -= value;
                }
            }
        }

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
        }

        /// <summary>
        /// Module string identifier.
        /// </summary>
        public abstract string StringID { get; }

        /// <summary>
        /// Module name in Guild config.
        /// </summary>
        public abstract string ModuleXmlName { get; }

        /// <summary>
        /// List containing active commands.
        /// </summary>
        public virtual IEnumerable<IBotCommand> ActiveCommands
        {
            get
            {
                foreach (var cmd in _cfgCommands)
                {
                    yield return cmd;
                }
                foreach (var cmd in _addCommands)
                {
                    yield return cmd;
                }
                foreach (var cmd in _useCommands)
                {
                    yield return cmd;
                }
                foreach (var cmd in _delCommands)
                {
                    yield return cmd;
                }
            }
        }

        /// <summary>
        /// ContentModule constructor.
        /// </summary>
        /// <param name="configEl">XElement containing guild's modules config.</param>
        /// <param name="adminIds">List of Guild Admins.</param>
        /// <param name="clientId">ID of Bot.</param>
        public ContentModule(XElement configEl, List<ulong> adminIds, ulong clientId)
        {
            _configEl = configEl ?? throw new ArgumentNullException("Configuration Element cannot be null.");

            _adminIds = adminIds;

            _clientId = clientId;

            if (_configEl.Element(ModuleXmlName) != null)
            {
                _moduleConfigEl = _configEl.Element(ModuleXmlName);
            }
            else
            {
                XElement moduleConfigEl =
                    new XElement(ModuleXmlName,
                        new XAttribute("on", Boolean.FalseString),
                        new XAttribute("prefix", _defaultPrefix));
                _moduleConfigEl = moduleConfigEl;

                _configEl.Add(moduleConfigEl);
            }

            Configure(_configEl);
        }

        /// <summary>
        /// Module reconfiguration method.
        /// </summary>
        /// <param name="configEl">XElement containing guild's modules config.</param>
        public void Reconfigure(XElement configEl)
        {
            if (configEl == null)
            {
                return;
            }

            Configure(configEl);
        }

        /// <summary>
        /// Extracts list of IDs from XAttribute.
        /// </summary>
        /// <param name="attr">XAttribute to extract IDs from.</param>
        /// <returns>List of IDs from specified XAttribute.</returns>
        protected List<ulong> ExtractPermissions(XAttribute attr)
        {
            List<ulong> ids = new List<ulong>();

            if (attr != null)
            {
                string permStringValue = attr.Value;
                string[] stringIds = permStringValue.Split(" ");
                if (stringIds.Length > 0)
                {
                    for (int i = 0; i < stringIds.Length; i++)
                    {
                        if (ulong.TryParse(stringIds[i], out ulong id))
                        {
                            ids.Add(id);
                        }
                    }
                }
            }

            return ids;
        }

        /// <summary>
        /// Module configuration method.
        /// </summary>
        /// <param name="configEl">XElement containing guild's modules config.</param>
        protected virtual void Configure(XElement configEl)
        {
            XElement moduleCfgEl = configEl.Element(ModuleXmlName);

            bool isActive = false;

            if (moduleCfgEl.Attribute("on") != null)
            {
                if (!Boolean.TryParse(moduleCfgEl.Attribute("on").Value, out isActive))
                {
                    isActive = false;
                }
            }

            _isActive = isActive;

            string prefix = _defaultPrefix;

            if (moduleCfgEl.Attribute("prefix") != null)
            {
                if (!String.IsNullOrWhiteSpace(moduleCfgEl.Attribute("prefix").Value))
                {
                    prefix = moduleCfgEl.Attribute("prefix").Value;
                }
            }

            _prefix = prefix;

            if (moduleCfgEl.Attribute("cfgPerm") == null)
            {
                moduleCfgEl.Add(new XAttribute("cfgPerm", ""));
            }
            if (moduleCfgEl.Attribute("addPerm") == null)
            {
                moduleCfgEl.Add(new XAttribute("addPerm", ""));
            }
            if (moduleCfgEl.Attribute("usePerm") == null)
            {
                moduleCfgEl.Add(new XAttribute("usePerm", ""));
            }
            if (moduleCfgEl.Attribute("delPerm") == null)
            {
                moduleCfgEl.Add(new XAttribute("delPerm", ""));
            }

            List<ulong> addPermissionList = ExtractPermissions(moduleCfgEl.Attribute("addPerm"));
            List<ulong> usePermissionList = ExtractPermissions(moduleCfgEl.Attribute("usePerm"));
            List<ulong> cfgPermissionList = ExtractPermissions(moduleCfgEl.Attribute("cfgPerm"));
            List<ulong> delPermissionList = ExtractPermissions(moduleCfgEl.Attribute("delPerm"));

            if (isActive)
            {
                GenerateCfgCommands(cfgPermissionList);
                GenerateUseCommands(usePermissionList);
                GenerateAddCommands(addPermissionList);
                GenerateDelCommands(delPermissionList);
            }
            else
            {
                _cfgCommands = new List<IBotCommand>();
                _useCommands = new List<IBotCommand>();
                _addCommands = new List<IBotCommand>();
                _delCommands = new List<IBotCommand>();
            }
        }

        /// <summary>
        /// Generates module configuration commands from specified list of allowed role IDs.
        /// </summary>
        /// <param name="perms">List of Roles IDs which are allowed to use Config commands.</param>
        protected virtual void GenerateCfgCommands(List<ulong> perms)
        {
            List<ulong> allPerms = new List<ulong>(_adminIds);
            allPerms.AddRange(perms);
            Rule cmdRule = RuleGenerator.HasRoleByIds(allPerms)
                & RuleGenerator.PrefixatedCommand(_prefix, "cfg");

            IBotCommand configCmd =
                new BotCommand(
                    StringID + "-configcmd",
                    cmdRule,
                    ConfigCommand
                );

            _cfgCommands = new List<IBotCommand> { configCmd };
        }

        /// <summary>
        /// Generates content addition commands from specified list of allowed role IDs.
        /// </summary>
        /// <param name="perms">List of Roles IDs which are allowed to use Add commands.</param>
        protected virtual void GenerateAddCommands(List<ulong> perms)
        {
            List<ulong> allPerms = new List<ulong>(_adminIds);
            allPerms.AddRange(perms);
            Rule addRule = RuleGenerator.HasRoleByIds(allPerms)
                & RuleGenerator.PrefixatedCommand(_prefix, "add");

            IBotCommand addCmd =
                new BotCommand(
                    StringID + "-addcmd",
                    addRule,
                    AddCommand
                );

            _addCommands = new List<IBotCommand> { addCmd };
        }


        /// <summary>
        /// Generates content deletion commands from specified list of allowed role IDs.
        /// </summary>
        /// <param name="perms">List of Roles IDs which are allowed to use Delete commands.</param>
        protected virtual void GenerateDelCommands(List<ulong> perms)
        {
            List<ulong> allPerms = new List<ulong>(_adminIds);
            allPerms.AddRange(perms);
            Rule delRule = RuleGenerator.HasRoleByIds(allPerms)
                & RuleGenerator.PrefixatedCommand(_prefix, "del");

            IBotCommand delCmd =
                new BotCommand(
                    StringID + "-delcmd",
                    delRule,
                    DeleteCommand
                );

            _delCommands = new List<IBotCommand> { delCmd };
        }

        /// <summary>
        /// Generates content usage commands from specified list of allowed role IDs.
        /// </summary>
        /// <param name="perms">List of Roles IDs which are allowed to use Use commands.</param>
        protected virtual void GenerateUseCommands(List<ulong> perms)
        {
            List<IBotCommand> useCommands = new List<IBotCommand>();

            var keys = _moduleConfigEl.Elements("key");

            foreach (var keyEl in keys)
            {
                if (keyEl.Attribute("name") == null)
                {
                    continue;
                }

                string key = keyEl.Attribute("name").ToString();

                List<ulong> allUsePerms = new List<ulong>(_adminIds);
                allUsePerms.AddRange(perms);
                Rule useRule = RuleGenerator.HasRoleByIds(allUsePerms)
                    & RuleGenerator.PrefixatedCommand(_prefix, key)
                    & (!RuleGenerator.UserByID(_clientId));

                useCommands.Add(
                    new BotCommand(
                        StringID + "-" + key + "-usecmd",
                        useRule,
                        UseCommandGenerator(keyEl.Value)
                    )
                );
            }

            List<ulong> allHelpPerms = new List<ulong>(_adminIds);
            allHelpPerms.AddRange(perms);
            Rule helpRule = RuleGenerator.HasRoleByIds(allHelpPerms)
                & RuleGenerator.PrefixatedCommand(_prefix, "help");

            useCommands.Add(
                new BotCommand(
                    StringID + "-helpcmd",
                    helpRule,
                    HelpCommand
                )
            );

            List<ulong> allListPerms = new List<ulong>(_adminIds);
            allListPerms.AddRange(perms);
            Rule listRule = RuleGenerator.HasRoleByIds(allListPerms)
                & RuleGenerator.PrefixatedCommand(_prefix, "list");

            useCommands.Add(
                new BotCommand(
                    StringID + "-listcmd",
                    listRule,
                    ListCommand
                )
            );

            _useCommands = useCommands;
        }

        /// <summary>
        /// Module configuration command.
        /// </summary>
        /// <param name="msg">SocketMessage containing command.</param>
        /// <returns>Async Task performing configuration.</returns>
        protected virtual async Task ConfigCommand(SocketMessage msg)
        {
            string content = msg.Content;
            string[] words = content.Split(" ");
            if (words.Length > 3)
            {
                switch (words[1])
                {
                    case "perm":
                        await PermissionControlCommand(words[2], msg);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Permission control command. Changes commands usage permissions on per-role basis.
        /// </summary>
        /// <param name="category">Command to change access to. Valid values - cfg, add, use, del.</param>
        /// <param name="msg">SocketMessage containing command.</param>
        /// <returns>Async Task perfrorming permissions change.</returns>
        protected virtual async Task PermissionControlCommand(string category, SocketMessage msg)
        {
            var roles = msg.MentionedRoles;
            List<ulong> rolesIds = new List<ulong>();

            foreach (var role in roles)
            {
                rolesIds.Add(role.Id);
            }

            switch (category)
            {
                case "cfg":
                    await ModifyPermissions(_moduleConfigEl.Attribute("cfgPerm"), rolesIds);
                    GenerateCfgCommands(rolesIds);
                    break;
                case "add":
                    await ModifyPermissions(_moduleConfigEl.Attribute("addPerm"), rolesIds);
                    GenerateAddCommands(rolesIds);
                    break;
                case "use":
                    await ModifyPermissions(_moduleConfigEl.Attribute("usePerm"), rolesIds);
                    GenerateUseCommands(rolesIds);
                    break;
                case "del":
                    await ModifyPermissions(_moduleConfigEl.Attribute("delPerm"), rolesIds);
                    GenerateDelCommands(rolesIds);
                    break;
                default:
                    break;
            }

            await msg.Channel.SendMessageAsync("Дозволи було змінено " + EmojiCodes.Picardia);
        }

        /// <summary>
        /// Modifies access to specified command, allowing specified role IDs usage of command.
        /// </summary>
        /// <param name="attr">XAttribute specifing permission.</param>
        /// <param name="ids">List of Role IDs allowed to use command.</param>
        /// <returns>Async Task performing permissions change.</returns>
        protected virtual async Task ModifyPermissions(XAttribute attr, List<ulong> ids)
        {
            string newValue = String.Empty;

            foreach (var id in ids)
            {
                newValue += id.ToString();
            }

            attr.Value = newValue;

            await RaiseConfigChanged(_configEl);
        }

        /// <summary>
        /// Config Changed Event wrapper.
        /// </summary>
        /// <param name="configEl">New configuration.</param>
        /// <returns>Async task performing config change.</returns>
        protected async Task RaiseConfigChanged(XElement configEl)
        {
            await Task.Run(
                () =>
                {
                    _configChanged(this, new ConfigChangedArgs(configEl));
                }
            );
        }

        /// <summary>
        /// Abstract method for content Addition command.
        /// </summary>
        /// <param name="msg">Message containing command invocation.</param>
        /// <returns>Async Task performing content Addition.</returns>
        protected abstract Task AddCommand(SocketMessage msg);

        /// <summary>
        /// Abstract method for content Addition command.
        /// </summary>
        /// <param name="msg">Message containing command invocation.</param>
        /// <returns>Async Task performing content Deletion.</returns>
        protected abstract Task DeleteCommand(SocketMessage msg);

        /// <summary>
        /// Abstract method for Help command.
        /// </summary>
        /// <param name="msg">Message containing command invocation.</param>
        /// <returns>Async Task performing Help function.</returns>
        protected abstract Task HelpCommand(SocketMessage msg);

        /// <summary>
        /// Abstract method for providing List of user uploaded content.
        /// </summary>
        /// <param name="msg">Message containing command invocation.</param>
        /// <returns>Async Task performing List function.</returns>
        protected abstract Task ListCommand(SocketMessage msg);

        /// <summary>
        /// Abstract method for content Usage command generator.
        /// </summary>
        /// <param name="key">Key by which content should be accessed.</param>
        /// <returns>Function which provides content accessed by specified key.</returns>
        protected abstract Func<SocketMessage, Task> UseCommandGenerator(string key);

    }
}
