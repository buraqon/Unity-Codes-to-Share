using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

public class Player : MonoBehaviour
{
    public PlayerEquipmentHandler playerEquipmentHandler;
    public PlayerMovementHandler playerMovementHandler;
    public PlayerHealthHandler playerHealthHandler;
    public RuntimeAnimatorController[] animationControllers;
    public PlayerState state;

    public AbilityDatabaseObject abilityDatabase;
    public WeaponType playerWeapon = 0;
    public Abilities abilities;

    public Transform weaponTransform;
    public Transform offHandWristTransform;
    public Transform offHandHandTransform;
    public GameObject[] toolHolder;
    public GameObject[] particleHolder;

    [SerializeField] public PlayerAttributes[] attributes;
    [SerializeField] public LayerMask layerMask;

    public InventoryObject inventory;
    public InventoryObject equipment;
    
    public Animator anim;
    public Collider weapon;
    public Collider[] collidChildren;
    public StarterAssetsInputs inputs;
    public Slider[] sliders;
    public Util util;
    private Quaternion mouseRotation = Quaternion.identity;

    void Start()
    {   
        inventory.Load();
        equipment.Load();

        playerMovementHandler  = new PlayerMovementHandler();
        playerHealthHandler = new PlayerHealthHandler(attributes, this.gameObject);
        playerEquipmentHandler = new PlayerEquipmentHandler();

        GameManager.instance.HUD_Object.StartHUD();
        
        state = new ReadyState();
        anim = GetComponent<Animator> ();
        anim.runtimeAnimatorController = animationControllers[(int) playerWeapon];
        
        util = new Util(layerMask, GameManager.instance.Cam, this.transform);

        collidChildren = util.GetAllChildrenColliders(transform);
        weapon = util.GetCollidWithTag("weapon");

        inputs = GetComponent<StarterAssetsInputs> ();
        sliders = playerHealthHandler.statsCircle.GetComponentsInChildren <Slider> ();
        abilities = new Abilities(this.gameObject, anim);

        playerEquipmentHandler.BeginEquipment();
        GameManager.instance.HUD_Object.inventoryScreen.gameObject.SetActive(false);
    }

    void Update()
    {
        if(!GameManager.instance.GameOver){
            playerHealthHandler.UpdateStats();
            state = state.handleInput();
        }
    }

    void OnTriggerEnter(Collider other){
        if(other.tag == "enemyWeapon"){
            float damage = 0;
            Enemy enemy= other.transform.root.GetComponent<Enemy> ();
            if(enemy != null)
            {
                damage = enemy.animal.damage;
            }
            else
            {
                damage = 20;
                Destroy(other.gameObject);
            }
            playerHealthHandler.TakeDamage(damage);
        }
    }
    
    void OnTriggerStay(Collider other){
        if(other.tag == "Pickable"){
            if(inputs.pick){ 
                var item = other.GetComponent<GroundItem>();
                if(item != null)
                {
                    Item _item = new Item(item.item);
                    if(inventory.AddItem(_item, 1))
                        Destroy(other.gameObject);
                }
           }             
        }
        if(other.tag == "gatherable" && state.ToString() == "ReadyState"){  
            if(inputs.pick)
                state = new GatheringState(other.gameObject.transform); 
        }
    }

    public void BeginAttack()
    {
        if(weapon != null)
            weapon.enabled = true;

        switch(playerWeapon)
        {
            case WeaponType.Bow:
                abilities.Shoot(abilityDatabase.BowObjects[0]);
                break;
            case WeaponType.Staff:
                abilities.Shoot(abilityDatabase.StaffObjects[0]);
                break;
            default:
                abilities.weapon.transform.GetChild(0).gameObject.SetActive(true);
                break;
        }
    }
    
    public void EndAttack()
    {
        if(weapon != null)
        {
            weapon.enabled = false;
            abilities.weapon.transform.GetChild(0).gameObject.SetActive(false);
        }
    }
    public void AttributeModified(PlayerAttributes attribute)
    {
        // Debug.Log(string.Concat(attribute.type, " was updated! Value is now ", attribute.Value.ModifiedValue));
    }

    private void  OnApplicationQuit()
    {
        inventory.Save();
        equipment.Save();
        inventory.Clear();
        equipment.Clear();
    }

    public void DestroyObject(GameObject objectToDestroy)
    {
        Destroy(objectToDestroy);
    }
    public Transform InstantiateItem(InventorySlot _slot, Transform tran)
    {
        Transform weaponObject;
        weaponObject = Instantiate(_slot.ItemObject.characterDisplay, tran).transform;
        return weaponObject;
    }
    public void UpdateAnimation()
    {
        anim.runtimeAnimatorController = animationControllers[(int)  playerWeapon];
        if((int)playerWeapon <= 1)
            anim.speed = 2f;
        else
            anim.speed = 1;
    }

    public void Slash(int slashIndex)
    {
        GameObject spawnedSlash = abilities.Shoot(abilityDatabase.GreatSwordObjects[0]);
        Destroy(spawnedSlash, 0.3f);
    }

    public void ShakeThing(GameObject _object, Vector3 _original)
    { 
        Vector3 newPos = _original + Random.insideUnitSphere*0.1f;
        newPos.y = _object.transform.position.y;
        _object.transform.position = newPos;
    }

    public void HitItemParticle(int i)
    {
        Quaternion rotation = Quaternion.Euler(new Vector3(-90, 0,0));
        GameObject particles = Instantiate(particleHolder[i], playerEquipmentHandler._toolHand.position, rotation);
    }
}