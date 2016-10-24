﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

/// <summary>
/// Represents a player (or client).  A player controls more than one character.  This class represents all characters controlled
/// by that given player.
/// </summary>
public class Player : MonoBehaviour
{
    public GameObject[] CharacterPrefabs = new GameObject[4];

    private GameObject[] characters = new GameObject[4];
    private GameObject selectedCharacter;

    public GameObject SelectedCharacter
    {
        get { return selectedCharacter;  }
        set { 
			if (selectedCharacter != null) {
				HeroUtil.SetHeroMarkerEnablement(false, this.selectedCharacter, Statics.CHAR_MARKER_BLUE_FILLED);
				HeroUtil.SetHeroMarkerEnablement(true, this.selectedCharacter, Statics.CHAR_MARKER_BLUE);

            }
            HeroUtil.SetHeroMarkerEnablement(true, value, Statics.CHAR_MARKER_BLUE_FILLED);
            HeroUtil.SetHeroMarkerEnablement(false, value, Statics.CHAR_MARKER_BLUE);

            this.selectedCharacter = value; 
			this.selectedCharacter.GetComponent<Hero>().Selected();
		}
    }

    public GameObject[] Characters
    {
        get { return this.characters; }
    }

    void Start()
    {
        Vector3[] spawnPoints = getSpawnPoints();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            this.characters[i] = Instantiate(CharacterPrefabs[i], spawnPoints[i], Quaternion.identity) as GameObject;
			initializeAll(this.characters [i]);
        }
    }


	void initializeAll (GameObject character)
	{
		HeroUtil.SetHeroMarkerEnablement(true, character, Statics.CHAR_MARKER_BLUE);
		//TODO i'm sure i have to initialize some more things
	}

    void Update()
    {
        PlayerInput.Type playerInput = PlayerInput.getPlayerInput();

		// code below is only valid to be on a selected character
		if (this.selectedCharacter == null) {
			return;
		}

		var characterControl = this.selectedCharacter.GetComponent<CharacterControl>();

        if (playerInput == PlayerInput.Type.MOVE)
        {
			characterControl.Move();
        }
		else if (playerInput == PlayerInput.Type.SELECT) {
			characterControl.SetTarget();
		}
    }

    Vector3[] getSpawnPoints()
    {
        Vector3[] spawnPoints = new Vector3[4];
        spawnPoints[0] = new Vector3(15, 0, 0);
        spawnPoints[1] = new Vector3(15, 0, 15);
        spawnPoints[2] = new Vector3(0, 0, 15);
        spawnPoints[3] = new Vector3(0, 0, 0);
        return spawnPoints;
    }
}