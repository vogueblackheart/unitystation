﻿using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the ChatIcon and PlayerChatBubble.
/// Automatically checks PlayerPrefs to determine
/// the use of each one.
/// </summary>
public class PlayerChatBubble : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The text size when the player speaks like a normal person.")]
	private float bubbleSizeNormal = 8;

	[SerializeField]
	[Tooltip("The size of the chat bubble when the player has typed in all caps or ends the sentence with !!.")]
	private float bubbleSizeCaps = 12;

	[SerializeField]
	[Tooltip("The size of the chat bubble when starts the sentence with #.")]
	private float bubbleSizeWhisper = 6;

	/// <summary>
	/// The current size of the chat bubble, scaling chatBubble RectTransform.
	/// </summary>
	private float bubbleSize = 8;

	/// <summary>
	/// Different types of chat bubbles, which might be displayed differently.
	/// TODO Chat.Process.cs has to detect these types of text as well. This detection should be unified to unsure consistent detection.
	/// </summary>
	enum BubbleType
	{
		normal, // Regular text -> Regular bubble
		whisper, // # -> Smaller bubble
		caps, // All caps (with at least 1 letter) OR end sentence with !! -> Bigger bubble
		clown // Clown occupation -> Comic Sans. Which actually looks good on low resolutions. Hmm.
	}

	[SerializeField]
	private ChatIcon chatIcon;
	[SerializeField]
	private GameObject chatBubble;
	[SerializeField]
	private Text bubbleText;
	[SerializeField]
	private GameObject pointer;
	class BubbleMsg { public float maxTime; public string msg; public float elapsedTime = 0f; }
	private Queue<BubbleMsg> msgQueue = new Queue<BubbleMsg>();
	private bool showingDialogue = false;

	/// <summary>
	/// A cache for the cache bubble rect transform. For performance!
	/// </summary>
	private RectTransform chatBubbleRectTransform;

	/// <summary>
	/// The type of the current chat bubble.
	/// </summary>
	private BubbleType bubbleType = BubbleType.normal;

	void Start()
	{
		chatBubble.SetActive(false);
		bubbleText.text = "";
		chatBubbleRectTransform = chatBubble.GetComponent<RectTransform>();
	}

	void OnEnable()
	{
		EventManager.AddHandler(EVENT.ToggleChatBubbles, OnToggle);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.ToggleChatBubbles, OnToggle);
	}

	void Update()
	{
		// Update scale of the chat bubble.
		// TODO Optimization. Instead of doing this in update, it should be done when the player has changed the zoom level.
		if (showingDialogue)
		{
			updateChatBubbleScale();
		}
		// TODO Add comment to the following code (or remove it). Bubbles seem to work fine without it.
		if (transform.eulerAngles != Vector3.zero)
		{
			transform.eulerAngles = Vector3.zero;
		}
	}

	void OnToggle()
	{
		if (PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleKey) == 0)
		{
			if (showingDialogue)
			{
				StopCoroutine(ShowDialogue());
				showingDialogue = false;
				msgQueue.Clear();
				chatBubble.SetActive(false);
			}
		}
	}

	public void DetermineChatVisual(bool toggle, string message, ChatChannel chatChannel)
	{
		if (!UseChatBubble())
		{
			chatIcon.ToggleChatIcon(toggle);
		}
		else
		{
			AddChatBubbleMsg(message, chatChannel);
		}
	}

	/// <summary>
	/// Determines the bubble type appropriate from the given message.
	/// Refer to BubbleType for further information.
	/// TODO Currently messages such as 
	/// </summary>
	/// <param name="msg"></param>
	private BubbleType GetBubbleType(string msg)
	{
		if (msg.Substring(0, 1).Equals("#")){
			return BubbleType.whisper;
		}
		if (msg.Substring(0, msg.Length - 2).Equals("!!")
			|| ((msg.ToUpper(CultureInfo.InvariantCulture) == msg) && msg.All(System.Char.IsLetter)))
		{
			return BubbleType.caps;
		}
		// TODO Clown occupation check & Somic Sans.

		return BubbleType.normal;
	}


	private void AddChatBubbleMsg(string msg, ChatChannel channel)
	{
		int maxcharLimit = 52;

		if (msg.Length > maxcharLimit)
		{
			while (msg.Length > maxcharLimit)
			{
				int ws = -1;
				//Searching for the nearest whitespace
				for (int i = maxcharLimit; i >= 0; i--)
				{
					if (char.IsWhiteSpace(msg[i]))
					{
						ws = i;
						break;
					}
				}
				//Player is spamming with no whitespace. Cut it up
				if (ws == -1 || ws == 0)ws = maxcharLimit + 2;

				var split = msg.Substring(0, ws);
				msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(split.Length), msg = split });

				msg = msg.Substring(ws + 1);
				if (msg.Length <= maxcharLimit)
				{
					msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg });
				}
			}
		}
		else
		{
			msgQueue.Enqueue(new BubbleMsg { maxTime = TimeToShow(msg.Length), msg = msg });
		}

		if (!showingDialogue)StartCoroutine(ShowDialogue());
	}

	IEnumerator ShowDialogue()
	{
		showingDialogue = true;
		if (msgQueue.Count == 0)
		{
			yield return WaitFor.EndOfFrame;
			yield break;
		}
		var b = msgQueue.Dequeue();
		SetBubbleText(b.msg);

		while (showingDialogue)
		{
			yield return WaitFor.EndOfFrame;
			b.elapsedTime += Time.deltaTime;
			if (b.elapsedTime >= b.maxTime)
			{
				if (msgQueue.Count == 0)
				{
					bubbleText.text = "";
					chatBubble.SetActive(false);
					showingDialogue = false;
				}
				else
				{
					b = msgQueue.Dequeue();
					SetBubbleText(b.msg);
				}
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	/// <summary>
	/// Sets the text of the bubble and updates the text bubble type.
	/// </summary>
	/// <param name="msg"> Player's chat message </param>
	private void SetBubbleText(string msg)
	{
		chatBubble.SetActive(true);
		bubbleText.text = msg;
		bubbleType = GetBubbleType(msg);
		switch (bubbleType)
		{
			case BubbleType.caps:
				bubbleSize = bubbleSizeCaps;
				break;
			case BubbleType.whisper:
				bubbleSize = bubbleSizeWhisper;
				break;
			case BubbleType.clown:
				// TODO Implement clown-specific bubble values.
				bubbleSize = bubbleSizeNormal;
				break;
			case BubbleType.normal:
			default:
				bubbleSize = bubbleSizeNormal;
				break;
		}
		updateChatBubbleScale();
	}

	/// <summary>
	/// Updates the scale of the chat bubble canvas using bubbleScale and the player's zoom level.
	/// </summary>
	private void updateChatBubbleScale()
	{
		int zoomLevel = PlayerPrefs.GetInt(PlayerPrefKeys.CamZoomKey);
		float bubbleScale = bubbleSize / zoomLevel;
		chatBubbleRectTransform.localScale = new Vector3(bubbleScale, bubbleScale, 1);
	}

	/// <summary>
	/// Used to calculate showing length time
	/// </summary>
	private float TimeToShow(int charCount)
	{
		return Mathf.Clamp((float)charCount / 10f, 2.5f, 10f);
	}

	/// <summary>
	/// Show the ChatBubble or the ChatIcon
	/// </summary>
	private bool UseChatBubble()
	{
		if (!PlayerPrefs.HasKey(PlayerPrefKeys.ChatBubbleKey))
		{
			PlayerPrefs.SetInt(PlayerPrefKeys.ChatBubbleKey, 0);
			PlayerPrefs.Save();
		}

		return PlayerPrefs.GetInt(PlayerPrefKeys.ChatBubbleKey) == 1;
	}
}