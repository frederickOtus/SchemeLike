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
    public InputField editor;
    public Button executeButton;

    public void Start()
    {
        Button btn = executeButton.GetComponent<Button>();
        btn.onClick.AddListener(ExecuteCode);
    }


    public void ExecuteCode()
    {

        var vm = SchemeMachine.instance.vm;
        editor.Select();
        string code = editor.text;
        vm.parseCode(editor.text);
        if (vm.parser.isDone())
        {
            foreach(var sxp in vm.parser.flushParseTree())
            {
                SchemeMachine.instance.pendingCode.Add(sxp);
            }
        }
        editor.text = "";
    }
}
