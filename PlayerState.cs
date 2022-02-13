using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    protected Player player;
    protected Util util;
    protected float timer;
    protected float period;

    // Constructors does everthing that an ability needs to do at first
    public PlayerState()
    {
        player = GameManager.instance.Player.GetComponent<Player>();
        util = new Util(player.layerMask, GameManager.instance.Cam, player.transform);
    }
    
    // Handle input for the repeatable stuff until the state is left!
    virtual public PlayerState handleInput()
    {
        return this;
    }

    protected void InitializeTimer(float _period)
    {
        timer = 0;
        period = _period;
    }

    protected bool IsTimeDone()
    {
        timer += Time.deltaTime;
        return (timer >= period);
    }
}

public class AliveState : PlayerState
{
    public override PlayerState handleInput()
    {
        if(!player.playerHealthHandler.isAlive)
            return new DeadState();

        return this;
    }
}

public class ReadyState : AliveState
{
    public ReadyState()
    {
        player.playerMovementHandler.SetMovementState(true);
    }
    public override PlayerState handleInput()
    {
        base.handleInput();
        if(!player.anim.GetCurrentAnimatorStateInfo(0).IsName("AbilityFinish"))
        {
            if(util.mouse.leftButton.isPressed)
                return new BasicAttack();

            if(util.mouse.rightButton.isPressed)
                return new BasicAbility();

            if(player.inputs.specialAbility)
                return new SpecializedAbiltiy();

            if(player.inputs.jump)
                return new RollingState();
        }
        return this;
    }
}

public class RollingState : AliveState
{
    public RollingState()
    {
        player.playerMovementHandler.SetMovementState(false);
        player.anim.Play("Roll");
        player.inputs.jump = false;
    }
    public override PlayerState handleInput()
    {
        base.handleInput();

        if(!player.anim.GetCurrentAnimatorStateInfo(0).IsName("Roll"))
        {
            player.playerMovementHandler.SetMovementState(false);
            return new ReadyState();
        }
        return this;
    }
}

public class AttackingState : AliveState
{
    public override PlayerState handleInput()
    {
        base.handleInput();

        if(IsAttackFinished())
            return new ReadyState();
        return this;
    }
    public bool IsAttackFinished()
    {
        return player.anim.GetCurrentAnimatorStateInfo(0).IsName("Idle");
    }
}

public class BasicAttack : AttackingState
{
    public BasicAttack()
    {
        player.playerMovementHandler.RotateWithMouse();
        player.abilities.Attack();   
    }
}

public class BasicAbility : AttackingState
{
    static Task currentTask;
    static GameObject spawnedObject;
    
    public BasicAbility()
    { 
        player.playerMovementHandler.RotateWithMouse();
        player.abilities.weapon.transform.GetChild(0).gameObject.SetActive(true);
        currentTask = player.abilities.BasicAbiltiy(ref spawnedObject);
    }
    public override PlayerState handleInput()
    {
        if(player.abilities.BasicAbiltiyOn(currentTask,spawnedObject))
            {
                player.abilities.weapon.transform.GetChild(0).gameObject.SetActive(false);
                return new ReadyState();
            }  
        return this;
    }
}

public class  SpecializedAbiltiy : AttackingState
{
    static Task currentTask;
    static GameObject spawnedObject;
    public SpecializedAbiltiy()
    {
        player.playerMovementHandler.RotateWithMouse();
        currentTask = player.abilities.SpecializedAbiltiy(ref spawnedObject);
    }

    public override PlayerState handleInput()
    {
        if(player.abilities.SpecializedAbiltiyOn(currentTask,spawnedObject))
            {
                GameObject.Destroy(spawnedObject, 1f);
                return new ReadyState();
            }  
        return this;
    }
}

public class DeadState : PlayerState
{
    public DeadState()
    {
        GameManager.instance.GameOver = true;
        player.anim.Play("Die");
        player.playerMovementHandler.SetMovementState(false);
    }
    public override PlayerState handleInput()
    {
        return this;
    }
}

// Gathering State

public class GatheringState : AliveState
{
    Vector3 target;
    GameObject targetObject;
    GroundItem item;
    GameObject tool;
    public GatheringState(Transform _target)
    {
        Vector3 targetPos = _target.position;
        targetObject = _target.gameObject;
        target = _target.position;
        item = targetObject.GetComponent<GroundItem>();
        player.playerMovementHandler.SetMovementState(false);
        
        player.transform.LookAt(new Vector3(targetPos.x, player.transform.position.y, targetPos.z));
        ToolAnimator();
        
        tool = player.toolHolder[(int) item.item.materialType];
        player.playerEquipmentHandler.SetActiveWeapons(false);
        player.playerEquipmentHandler.EquipTool(tool);

        InitializeTimer(1f);
    }
    public override PlayerState handleInput()
    {
        player.ShakeThing(targetObject.gameObject, target);
        GameManager.instance.HUD_Object.FillProgressBar(timer/period);
        
        if(IsTimeDone())
        {
            targetObject.transform.position = target;
            player.playerEquipmentHandler.DestroyTool();
            player.playerEquipmentHandler.SetActiveWeapons(true);
                if(item != null)
                {
                    Item _item = new Item(item.item);
                    player.inventory.AddItem(_item, 5);
                    item.amount -= 1;
                    
                    GameManager.instance.HUD_Object.SpawnMaterialRecieved(item.item.uiDisplay,5);

                    if (item.amount <= 0){
                        GameObject.Destroy(targetObject);
                        ResourcesManager.instance.RemoveResource((int) item.item.materialType);
                    }
                        
                }
            return new ReadyState();
        }
        return this;
    }

    public void ToolAnimator()
    {
        int matType = (int) item.item.materialType;
        player.anim.SetInteger("GatherTool", matType);
        player.anim.Play("Gather");
    }
}