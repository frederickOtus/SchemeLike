using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DerpScheme;
using Completed;
using System;
using Environment = DerpScheme.Environment;

public class SchemeMachine : MonoBehaviour
{
    public Text outputDisplay;
    public DerpInterpreter vm;
    public List<SExpression> pendingCode;

    private IEnumerator<ExecutionMessage> currentlyProcessing;
    public static SchemeMachine instance;


    // Start is called before the first frame update
    void Start()
    {
    }

    void Awake()
    {
        //Check if instance already exists
        if (instance == null)

            //if not, set instance to this
            instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        pendingCode = new List<SExpression>();

        if (vm is null)
        {
            vm = new DerpInterpreter();

            vm.e.addVal("up", new SPrimitive(MoveUp, true, 0));
            vm.e.addVal("down", new SPrimitive(MoveDown, true, 0));
            vm.e.addVal("right", new SPrimitive(MoveRight, true, 0));
            vm.e.addVal("left", new SPrimitive(MoveLeft, true, 0));
            vm.e.addVal("where-am-i", new SPrimitive(PlayerLocation, true, 0));
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (currentlyProcessing == null || currentlyProcessing.Current.status == ExecStatus.DONE)
        {
            if (pendingCode.Count > 0)
            {
                currentlyProcessing = vm.evaluate(pendingCode[0], vm.e).GetEnumerator();
                pendingCode.RemoveAt(0);
            }

        }
        while (currentlyProcessing != null && currentlyProcessing.MoveNext())
        {
            if (currentlyProcessing.Current.status == ExecStatus.PENDING_PRIMATIVE)
                return;
            if (currentlyProcessing.Current.status == ExecStatus.DONE)
            {
                outputDisplay.text = currentlyProcessing.Current.returnVal.ToString();
            }

        }
    }


    #region functions to be added to the scheme interpreter
    private IEnumerable<ExecutionMessage> MoveUp(List<SExpression> args, Environment e)
    {
        Player.self.MovePlayer(0, 1);
        while (Player.self.isMoving)
        {
            yield return new ExecutionMessage(ExecStatus.PENDING_PRIMATIVE, new SEmptyList());
        }
        yield return new ExecutionMessage(ExecStatus.DONE, new SEmptyList());
    }

    private IEnumerable<ExecutionMessage> MoveDown(List<SExpression> args, Environment e)
    {
        Player.self.MovePlayer(0, -1);
        while (Player.self.isMoving)
        {
            yield return new ExecutionMessage(ExecStatus.PENDING_PRIMATIVE, new SEmptyList());
        }
        yield return new ExecutionMessage(ExecStatus.DONE, new SEmptyList());
    }

    private IEnumerable<ExecutionMessage> MoveLeft(List<SExpression> args, Environment e)
    {
        Player.self.MovePlayer(-1, 0);
        while (Player.self.isMoving)
        {
            yield return new ExecutionMessage(ExecStatus.PENDING_PRIMATIVE, new SEmptyList());
        }
        yield return new ExecutionMessage(ExecStatus.DONE, new SEmptyList());
    }

    private IEnumerable<ExecutionMessage> MoveRight(List<SExpression> args, Environment e)
    {
        Player.self.MovePlayer(1, 0);
        while (Player.self.isMoving)
        {
            yield return new ExecutionMessage(ExecStatus.PENDING_PRIMATIVE, new SEmptyList());
        }
        yield return new ExecutionMessage(ExecStatus.DONE, new SEmptyList());
    }

    private IEnumerable<ExecutionMessage> PlayerLocation(List<SExpression> args, Environment e)
    {
        var pv = Player.self.transform.position;
        var head = new SInt((int)Math.Round(pv.x, 0));
        var tail = new SInt((int)Math.Round(pv.y, 0));
        yield return new ExecutionMessage(ExecStatus.DONE, new SPair(head, tail));
    }

    private IEnumerable<ExecutionMessage> WorldInfo(List<SExpression> args, Environment e)
    {
        yield return null;
    }
    #endregion


}
