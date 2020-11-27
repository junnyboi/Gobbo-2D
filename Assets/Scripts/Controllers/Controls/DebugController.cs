using System;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{
    bool showConsole;
    bool showHelp;
    bool showHistory;

    public static DebugController Instance;
    public Dictionary<string, object> commandDict = new Dictionary<string, object>();
    string input;

    List<string> pastCommands = new List<string>();

    void Awake()
    {
        Instance = this;

        commandDict.Add("help",
                        new DebugCommand("Help Menu",
                            "Shows available commands.",
                            "help",
                            () =>
                            {
                                showHelp = !showHelp;
                            }));
        commandDict.Add("history",
                        new DebugCommand("Help Menu",
                            "Shows previous commands.",
                            "history",
                            () =>
                            {
                                showHistory = !showHistory;
                            }));
        commandDict.Add("clearHistory",
                        new DebugCommand("Help Menu",
                            "clears previous commands.",
                            "clearHistory",
                            () =>
                            {
                                pastCommands.Clear();
                            }));
        commandDict.Add("newWorld", 
                        new DebugCommand("New World",
                            "Create a new world, burn the old one.",
                            "newWorld",
                            () =>
                            {
                                WorldController.Instance.NewWorld();
                            } ));
        commandDict.Add("collectResource",
                        new DebugCommand("Collect Resource",
                            "Collects selected resource stacks into the stockpile immediately.",
                            "collectResource",
                            () =>
                            {
                                Debug.Log("Console command: collectResource");
                            }));
        commandDict.Add("addResource",
                        new DebugCommand("Add Resource",
                            "Adds resource of <type>.",
                            "addResource <string type>",
                            () =>
                            {
                                Debug.Log("Console command: addResource");
                            }));
        commandDict.Add("killAll",
                        new DebugCommand("Kill All",
                            "Removes all creatures from the scene.",
                            "killAll",
                            () =>
                            {
                                CreaturesController.Instance.RemoveAllCreatures();
                            }));
        commandDict.Add("createGoblyn",
                        new DebugCommand("Create Goblyn",
                            "Creates a new goblyn in the middle of the map.",
                            "createGoblyn",
                            () =>
                            {
                                CreaturesController.Instance.CreateGoblyn();
                            }));
        commandDict.Add("addMushroom",
                        new DebugCommand<int>("addMushroom",
                            "Adds <int amount> mushrooms to the stockpile.",
                            "addMushroom <int amount>",
                            (amt) =>
                            {
                                ResourceController.Instance.ChangeStockpile("Cup Mushroom", amt);
                            }));
    }

    public void OnToggleDebug()
    {
        showConsole = !showConsole;
    }

    public void OnReturn()
    {
        if (showConsole)
        {
            HandleInput();
            input = "";
        }
    }

    Vector2 scroll;

    private void OnGUI()
    {
        if (!showConsole) return;
        
        float y = 0f;
        float width = Screen.width;

        if (showHelp)
        {
            GUI.Box(new Rect(0, y, width, 100), "");

            Rect viewport = new Rect(0, 0, width - 30, 20 * commandDict.Count);
            scroll = GUI.BeginScrollView(new Rect(0, y + 5f, width, 90), scroll, viewport);

            int i = 0;
            foreach (object o in commandDict.Values)
            {
                DebugCommandBase command = o as DebugCommandBase;
                if (command == null) continue;
                i++;
                string label = $"{command.commandFormat} -- {command.commandDescription}";
                Rect labelRect = new Rect(5, 20*i, viewport.width - 100, 20);
                GUI.Label(labelRect, label);
            }

            GUI.EndScrollView();
            y += 100;
        }

        if (showHistory)
        {
            GUI.Box(new Rect(0, y, width, 100), "");

            Rect viewport = new Rect(0, 0, width - 30, 20 * commandDict.Count);
            scroll = GUI.BeginScrollView(new Rect(0, y + 5f, width, 90), scroll, viewport);

            int i = 0;
            foreach (string s in pastCommands)
            {
                i++;
                string label = s;
                Rect labelRect = new Rect(5, 20 * i, viewport.width - 100, 20);
                GUI.Label(labelRect, label);
            }

            GUI.EndScrollView();
            y += 100;
        }

        GUI.Box(new Rect(0, y, width, 30), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        if (Event.current.Equals(Event.KeyboardEvent("return"))) OnReturn();
        input = GUI.TextField(new Rect(10f, y + 5f, width - 20f, 20f), input);  
    }

    private void HandleInput()
    {
        string[] properties = input.Split(' ');

        if (input == null) return;

        if (commandDict.ContainsKey(input))
        {
            try
            {
                (commandDict[input] as DebugCommand).Invoke();
            }
            catch
            {
                if (commandDict.ContainsKey(properties[0]))
                {
                    (commandDict[properties[0]] as DebugCommand<int>).Invoke(int.Parse(properties[1]));
                }
            }
        }
        else
        {
            Debug.LogError("Console command for '" + input + "' does not exist.");
            return;
        }

        pastCommands.Add(input);
    }
}


public class DebugCommandBase
{
    private string _commandId;
    private string _commandDescription;
    private string _commandFormat;

    public string commandId { get { return _commandId; } }
    public string commandDescription { get { return _commandDescription; } }
    public string commandFormat { get { return _commandFormat; } }

    public DebugCommandBase(string id, string description, string format)
    {
        _commandId = id;
        _commandDescription = description;
        _commandFormat = format;
    }
}


public class DebugCommand : DebugCommandBase
{
    private Action command;

    public DebugCommand(string id, string description, string format, Action command):base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke()
    {
        command.Invoke();
    }

}


public class DebugCommand<T1> : DebugCommandBase
{
    private Action<T1> command;

    public DebugCommand(string id, string description, string format, Action<T1> command) : base(id, description, format)
    {
        this.command = command;
    }

    public void Invoke(T1 value)
    {
        command.Invoke(value);
    }

}
