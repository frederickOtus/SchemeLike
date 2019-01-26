using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DerpScheme;
using Completed;
using System;
using Environment = DerpScheme.Environment;

public class RunCode : MonoBehaviour
{
    public Text outputDisplay;
    public InputField editor;
    public Button executeButton;
    public GameObject player;
    private DerpInterpreter vm;

    public void Start()
    {
        Button btn = executeButton.GetComponent<Button>();
        btn.onClick.AddListener(ExecuteCode);

        vm = new DerpInterpreter();
        Player player = Player.self;

        Func left = delegate (List<SExpression> args, Environment e)
        {
            if (args.Count != 0)
                throw new Exception(String.Format("Expected 0 args, got {0}", args.Count));

            player.MovePlayer(-1,0);
            return new SEmptyList();
        };

        Func right = delegate (List<SExpression> args, Environment e)
        {
            if (args.Count != 0)
                throw new Exception(String.Format("Expected 0 args, got {0}", args.Count));

            player.MovePlayer(1, 0);
            return new SEmptyList();
        };

        Func up = delegate (List<SExpression> args, Environment e)
        {
            if (args.Count != 0)
                throw new Exception(String.Format("Expected 0 args, got {0}", args.Count));

            player.MovePlayer(0, 1);
            return new SEmptyList();
        };

        Func down = delegate (List<SExpression> args, Environment e)
        {
            if (args.Count != 0)
                throw new Exception(String.Format("Expected 0 args, got {0}", args.Count));

            player.MovePlayer(0,-1);
            return new SEmptyList();
        };

        vm.addPrimative("up", new SPrimitive(up));
        vm.addPrimative("down", new SPrimitive(down));
        vm.addPrimative("left", new SPrimitive(left));
        vm.addPrimative("right", new SPrimitive(right));
    }

    public void ExecuteCode()
    {
        editor.Select();
        string code = editor.text;
        string res = vm.interpret(code);
        outputDisplay.text = res;
        editor.text = "";
    }
}
