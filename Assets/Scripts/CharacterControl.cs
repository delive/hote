﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Character control for an individual hero.
/// 
/// Synchronization based on https://unity3d.com/learn/tutorials/topics/multiplayer-networking/introduction-simple-multiplayer-example?playlist=29690
/// </summary>
public class CharacterControl : MonoBehaviour {
    private static string ANIM_STATE = "animation";
    private static Dictionary<AttackType, Anim> attackAnimMap = generateAttackTypeToAnimMap();

	public LayerMask heroLayerMask;

    // The Animator is what controls the switching from one animator to the other
    private Animator anim;
    private NavMeshAgent navMesh;
	private GameObject locationPointer;

	// player controlled walking
	private bool isPlayerForcedWalking;
	// autoattack seek walking - trying to get in range to autoattack
	private bool isSeekWalking;

    // gameobject who this player is currently targeting
    private GameObject playerTarget;

	// bookmark to know we've seen the hero as dead. the death source of truth is the hero
	//TODO remove this
	private bool charIsDead;

	// determines if an attack is overriding the UI. this disables certain actions like SELECT
	private bool attackUIOverride;

	private Hero hero;


    /// <summary>
    /// Initialization of the script
    /// </summary>
    void Start()
    {
        this.anim = GetComponentInChildren<Animator>();
        this.navMesh = GetComponent<NavMeshAgent>();
		this.hero = GetComponent<Hero> ();
		this.locationPointer = GameObject.FindWithTag("location_pointer");

        // characters can walk through each other
        Physics.IgnoreLayerCollision(8, 8);
    }

    /// <summary>
    /// Character control's main body of work is moving, animating, and delegating to the hero for attacking.
	/// Controlling a animation state machine frame by frame is hard. The easiest way to ensure one state change
	/// per frame is to always exit doUpdate() immediately on each state change. No Update() call for this class,
    /// only doUpdate, which is called on the Player Update().  This avoids two separate animation changes in one frame.
    /// </summary>
    public void doUpdate()
    {
		if (isDead()) {
			return;
		}

		if (isPlayerForcedWalking)
        {
            StopPlayerWalkIfAtDestination();
			return;
        }

		if (this.playerTarget == null || this.hero.IsAttacking()) {
			return;
		}

		if (this.playerTarget.GetComponent<Hero>().isDead()) {
			if (anim.GetCurrentAnimatorStateInfo(0).IsName("AutoAttacks") && !anim.IsInTransition(0)) {
				this.anim.SetTrigger(Anim.IDLE.ToString());
			}
			return;
		}

		if (this.hero.OutOfRange(this.playerTarget)) {
			// move towards target until we're in range
			this.navMesh.destination = this.playerTarget.transform.position;

			if (!this.isSeekWalking) {
				// we just started seek walking
				this.anim.SetTrigger(Anim.WALK.ToString());
				this.isSeekWalking = true;
			}
			return;
		}
		else if (this.isSeekWalking) {
			// we were just seekwalking, but now we're close enough to the target to autoattack
			StopWalking();
		}

		if (RotateTowards(this.playerTarget.transform)) {
			// need to rotate more before we can do anything
			return;
		}

		if (!this.hero.IsAttacking()) {
			StartAutoAttackAnimation();
			this.hero.StartAutoAttack(this.playerTarget);
		}
    }

	void StopWalkingAndAnimation ()
	{
		StopWalking();
		this.anim.SetTrigger(Anim.IDLE.ToString());
	}

	/// <summary>
	/// Hacky method. This should simply be represented by this.hero.IsDead(). However,
	/// there seems to be a bug with Unity (or more likely, with our code) that prevents us from
	/// transitioning from Walking to Dead in one frame.  This block of code effectively gives us 1 frame extra
	/// to transition from Walking->Idle and then AnyState->Dead which appears to work.
	/// </summary>
	bool isDead ()
	{
		if (this.charIsDead) {
			if (this.anim.GetInteger(ANIM_STATE) != (int)Anim.DEATH) {
				this.anim.SetTrigger(Anim.DEATH.ToString());
			}
		}

		if (this.hero.isDead() && !this.charIsDead) {
			this.charIsDead = true;
			if (isWalking()) {
				StopWalkingAndAnimation();
			}
		}
		return this.charIsDead;
	}

	bool isWalking ()
	{
		return this.isPlayerForcedWalking || this.isSeekWalking;
	}

	private void StartAutoAttackAnimation ()
	{
		// start autoattack animation if we weren't previously autoattacking
		if (this.anim.GetInteger(ANIM_STATE) != (int)Anim.AUTOATTACK) {
			this.anim.SetTrigger(Anim.AUTOATTACK.ToString());
		}
		else {
			// if we're already autoattack, we need to reset the animation
			this.anim.Play("AutoAttacks", -1, 0f);
		}
	}

	private bool RotateTowards (Transform target) {
		Vector3 direction = (target.position - transform.position).normalized;
		Quaternion lookRotation = Quaternion.LookRotation(direction);
		float angle = Quaternion.Angle(transform.rotation, lookRotation);

		if (angle < 5f) {
			// slerping is strange. give up rotating if we're less than 5 degrees angled from target
			// to smooth out (and speed up) the rotation transition
			return false;
		}

		transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
		return true;
	}

	/// <summary>
	/// Player has selected something for this character
	/// </summary>
    public void Select()
    {
		if (this.hero.isDead() || this.attackUIOverride) {
			return;
		}

		//TODO this causes bugs with ui overrides from abilities. select gets called
		// at the same time. select should also stop attacks. but, we need to disable this
		// when in ui override.

		// we've selected a hero
        RaycastHit hitInfo = new RaycastHit();
		if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, 200, this.heroLayerMask))
        {
			//TODO avoid targetting friends too!

			// can't target ourselves
			var targetted = hitInfo.collider.gameObject;
			if (targetted.GetInstanceID() == gameObject.GetInstanceID()) {
				return;
			}

			if (isWalking()) {
				StopWalkingAndAnimation();
			}

			// haven't selected anyone yet, just accept the new target
			if (playerTarget == null) {
				playerTarget = hitInfo.collider.gameObject;
				return;
			}

			// targetting the same target, nothing to do
			if (targetted.GetInstanceID() == playerTarget.GetInstanceID()) {
				return;
			}
				
			// only switch to another target if we can stop attacking
			if (GetComponent<Hero>().StopAttack()) {
				playerTarget = hitInfo.collider.gameObject;

				Debug.Log("new target " + playerTarget);
			}
        }
    }

    private void StopPlayerWalkIfAtDestination()
    {
        // navmesh is buggy. need to check alot to determine that we're done walking (or reached destination)
        if ((!this.navMesh.pathPending)
            && (this.navMesh.remainingDistance <= this.navMesh.stoppingDistance)
            && (!this.navMesh.hasPath || this.navMesh.velocity.sqrMagnitude == 0f))
        {
			StopWalkingAndAnimation();
        }
    }

    private void StopWalking()
    {
        this.navMesh.Stop();
        this.navMesh.ResetPath();
		this.isPlayerForcedWalking = false;
		this.isSeekWalking = false;
    }

    /// <summary>
    /// Moves the player in the right direction and also rotates them to look at the target position.
    /// When the player gets to the target position, stop them from moving.
    /// </summary>

    public void Move()
    {
		if (this.hero.isDead() || this.hero.IsImmobile()) {
			return;
		}

		// attempt to stop attacking (if attacking) so we can move
		if (!this.hero.StopAttack()) {
			// can't stop attacking so the move command is ignored
			return;
		}

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200))
        {
            navMesh.destination = hit.point;

			this.locationPointer.transform.position = navMesh.destination;
			this.locationPointer.GetComponent<Animator>().SetTrigger("mark");

            // might have already been walking. only start the animation if we just started
            if (!isPlayerForcedWalking)
            {
                isPlayerForcedWalking = true;
				this.anim.SetTrigger(Anim.WALK.ToString());
            }
        }
    }

    public void Attack(AttackType type)
    {
		if (this.hero.isDead() || this.hero.CooldownInProgress(type)) {
			return;
		}

		// try and stop attacking. if we can't, no new attack this frame
		if (this.hero.IsAttacking() && !this.hero.StopAttack()) {
			return;
		}

		bool attackSuccess = false;

		switch (type) {
		case AttackType.BLUE:
			attackSuccess = this.hero.BlueAttack();
			break;
		case AttackType.GREEN:
			attackSuccess = this.hero.GreenAttack();
			break;
		case AttackType.PURPLE:
			attackSuccess = this.hero.PurpleAttack();
			break;
		case AttackType.RED:
			attackSuccess = HasAliveTarget() ? this.hero.RedAttack(this.playerTarget) : false;
			break;
		default:
			throw new ArgumentException ("unknown attack: " + type);
		}

		if (attackSuccess) {
			StopWalking();

			// trigger the attack animation / substate
			this.anim.SetTrigger(attackAnimMap [type].ToString());
		}
    }

	public delegate void AttackUIOverrideFunction(bool enable);

	public AttackUIOverrideFunction GetAttackUIOverrideFunction ()
	{
		return AttackUIOverrideControl;
	}

	private void AttackUIOverrideControl(bool enable) {
		this.attackUIOverride = enable;
		GameObject.FindGameObjectWithTag("camera").GetComponent<CameraMovement>().SetUiOverride(enable);
	}

	private bool HasAliveTarget ()
	{
		return this.playerTarget != null && this.playerTarget.GetComponent<Hero>().IsAlive();
	}

    private static Dictionary<AttackType, Anim> generateAttackTypeToAnimMap()
    {
        Dictionary<AttackType, Anim> map = new Dictionary<AttackType, Anim>();
        map.Add(AttackType.GREEN, Anim.GREEN);
        map.Add(AttackType.BLUE, Anim.BLUE);
        map.Add(AttackType.RED, Anim.RED);
        map.Add(AttackType.PURPLE, Anim.PURPLE);
        return map;
    }

    public enum Anim { IDLE, WALK, AUTOATTACK, GREEN, BLUE, RED, PURPLE, DEATH}
}
