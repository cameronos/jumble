//Jumble mod for KTANE
//author: cameronos
//date: 11/5/2025
//listen to GOOD music /watch?v=zZJPN7ii-aA

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;
using UnityEngine.Networking;

public class JumbleScriptV2 : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMSelectable[] Buttons;
	 public TextMesh[] displayTexts;
   public Renderer moduleBG;
   public Material[] moduleMats;

	 static int jumbleNumber = 0;
	 static int displayNumber = 0;

   private const float _interactionPunchIntensity = .5f;
	 private int currentButtonIndex = 0;
	 private string[] activeSequence;
   private Coroutine flashCoroutine;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool doneFlashing = false;
   private bool ModuleSolved;

	 private readonly Dictionary<string, string[]> wordTable = new Dictionary<string, string[]>()
{
    { "SPARK", new string[]{ "B","B","D","C","A" } },
    { "LIGHT", new string[]{ "A","D","D","B","C" } },
    { "STORM", new string[]{ "C","A","D","D","B" } },
    { "CIRCUIT", new string[]{ "D","B","C","A","D","B" } },
    { "WELDER", new string[]{ "A","B","A","D","C" } },
    { "TUMBLE", new string[]{ "C","D","B","A","C" } },
    { "ENERGY", new string[]{ "D","C","A","B","B" } },
    { "STATIC", new string[]{ "A","D","C","B","D" } },
    { "FIRE", new string[]{ "B","D","A","C" } },
    { "POWER", new string[]{ "C","B","D","A","C" } },
    { "SHOCK", new string[]{ "A","B","C","D" } },
    { "FLAME", new string[]{ "B","C","B","D","A" } },
    { "SURGE", new string[]{ "C","A","D","B" } },
    { "TORCH", new string[]{ "D","B","A","C" } },
    { "WATT", new string[]{ "B","A","C","A","D" } },
    { "FLASH", new string[]{ "C","D","A","B","B" } },
    { "CHARGE", new string[]{ "D","A","C","C","B" } },
    { "BOLT", new string[]{ "A","C","B","D" } },
    { "SPARKLE", new string[]{ "B","B","D","A","C" } },
    { "REGULATOR", new string[]{ "C","A","B","D","D" } },
};

	public char[] buttonLetters = new char[4] { 'A', 'B', 'C', 'D'};
	private string[] displayWords = new string[5];   //words
	private string[] jumbledWords = new string[5];   //version shown

	private string confuseWord(string word)
	{
	    string jumbled;
	    do
	    {
	        char[] letters = word.ToCharArray();
	        for (int i = letters.Length - 1; i > 0; i--)
	        {
	            int j = Rnd.Range(0, i + 1);
	            char temp = letters[i];
	            letters[i] = letters[j];
	            letters[j] = temp;
	        }
	        jumbled = new string(letters);
	    } while (jumbled == word); //rare case it generates a randomized version of itself?
	    return jumbled;
	}

	private void setDisplays()
	{
	    var allWords = wordTable.Keys.ToList();
	    var usedIndices = new HashSet<int>();
	    for (int i = 0; i < 5; i++)
	    {
	        int index;
	        do
	        {
	            index = Rnd.Range(0, allWords.Count);
	        } while (usedIndices.Contains(index));
	        usedIndices.Add(index);
	        displayWords[i] = allWords[index];
	        jumbledWords[i] = confuseWord(displayWords[i]);

					if (displayTexts != null && i < displayTexts.Length && displayTexts[i] != null)
            displayTexts[i].text = jumbledWords[i];
	    }
	}

	private string[] GetButtonSequence(string word)
	{
	    if (!wordTable.ContainsKey(word))
	        return null;
	    var sequence = wordTable[word];
	    // Reverse sequence if jumbleNumber > 60
	    if (jumbleNumber > 60)
	        sequence = sequence.Reverse().ToArray();
	    return sequence;
	}

   void Awake () {
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;

			for (int i = 0; i < Buttons.Length; i++)
    {
        int buttonIndex = i; // Important: capture the index for the delegate
        Buttons[i].OnInteract += delegate ()
        {
            buttonPress(buttonIndex);
            return false;
        };
    }

		// Yc0gKJ4u-TU
		// Pyah!!!
    for (int i = 0; i < Buttons.Length; i++)
    {
        Buttons[i].AddInteractionPunch(_interactionPunchIntensity);
    }

   }

	 void buttonPress(int buttonIndex)
		{
      if(doneFlashing){
			  Audio.PlaySoundAtTransform("Press", displayTexts[0].transform);
		    char buttonLetter = buttonLetters[buttonIndex];
		    Debug.LogFormat("[Jumble #{0}] Button {1} was pressed.", ModuleId, buttonLetter);

		    if (activeSequence == null || ModuleSolved)
		        return;

		    if (buttonLetter.ToString() == activeSequence[currentButtonIndex])
		    {
		        currentButtonIndex++;
		        if (currentButtonIndex >= activeSequence.Length)
		        {
		            Debug.LogFormat("[Jumble #{0}] Correct sequence completed!", ModuleId);
		            Solve();
		        }
		    }
		    else
		    {
		        Debug.LogFormat("[Jumble #{0}] Wrong button! Strike!", ModuleId);
		        Strike();
		        currentButtonIndex = 0;
		    }
      }
		}

   void OnDestroy () { //Shit you need to do when the bomb ends

   }

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on

   }

	 public int GetCurrentJumbleNumber()
	     {
				  //when trying to implement this as a WebRequest, I quickly learned how
					//grabbing an updated DIV was impossible. this is a jerryrigged remake of the original
					//number generator in the script.js of the site.
					//I think it works for the most part, and it only runs on init, so maybe OK?
	         DateTime now = DateTime.UtcNow;
	         DateTime epoch = new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc);
	         long totalMilliseconds = (long)(now - epoch).TotalMilliseconds;
	         long currentBlock = totalMilliseconds / 60000;
	         double seed = (currentBlock * 2654435761.0) % 9973;
	         int number = (int)((seed % 100) + 1);
	         return number;
	     }

       private IEnumerator flashBackground()
       {
           float pulseSpeed = 2f; // Full pulse cycle in seconds

           while (true)
           {
               float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
               moduleBG.material = t > 0.5f ? moduleMats[1] : moduleMats[0];
               yield return null;
           }
       }

       private void resetBackgroundColor()
       {
           if (moduleBG != null && moduleMats != null && moduleMats.Length > 0)
               moduleBG.material = moduleMats[0]; // Set back to normal material
       }


   private IEnumerator AlarmForCurrentNumber()
   {
       doneFlashing = false;
       jumbleNumber = GetCurrentJumbleNumber();
       Debug.LogFormat("[Jumble #{0}] Jumble number is {1}.", ModuleId, jumbleNumber);
       Audio.PlaySoundAtTransform("Alarm", displayTexts[0].transform);
       if (displayTexts != null && displayTexts.Length >= 5)
       {
           displayTexts[0].text = "THE";
           displayTexts[1].text = "JUMBLE";
           displayTexts[2].text = "NUMBER";
           displayTexts[3].text = "IS";
           displayTexts[4].text = jumbleNumber.ToString();
       }
       flashCoroutine = StartCoroutine(flashBackground());
       yield return new WaitForSeconds(8);
       if (flashCoroutine != null)
               StopCoroutine(flashCoroutine);
           resetBackgroundColor();
       displayNumber = (jumbleNumber % 5) + 1;
       Debug.LogFormat("[Jumble #{0}] Display number is {1}.", ModuleId, displayNumber);
       setDisplays();
       string activeWord = displayWords[displayNumber - 1];
       Debug.LogFormat("[Jumble #{0}] Active word is {1}.", ModuleId, activeWord);
       activeSequence = GetButtonSequence(activeWord);
       currentButtonIndex = 0;
       Debug.LogFormat("[Jumble #{0}] Jumble Number is {1} ({2})",
           ModuleId,
           jumbleNumber,
           jumbleNumber > 60 ? "above 60 → sequence reversed" : "NOT above 60 → sequence is not reversed");
       Debug.LogFormat("[Jumble #{0}] Active sequence to input for {1}: {2}",
           ModuleId, activeWord, string.Join(", ", activeSequence));
      doneFlashing = true;
   }

	 void Start()
			 {
           StartCoroutine(AlarmForCurrentNumber());
     }

   void Update () { //Shit that happens at any point after initialization
   }

   private IEnumerator AnimateSolveText()
   {
       Audio.PlaySoundAtTransform("ChangeFlaps", displayTexts[0].transform);
       float flipDelay = 0.7f;
       int randomSteps = 2;

       char[][] displayedChars = new char[displayTexts.Length][];
       for (int i = 0; i < displayTexts.Length; i++)
       {
           if (displayTexts[i] == null || i >= displayWords.Length)
               continue;
           displayedChars[i] = jumbledWords[i].ToCharArray();
       }

       int maxLength = displayWords.Max(w => w.Length);

       for (int letterIndex = 0; letterIndex < maxLength; letterIndex++)
       {
           for (int i = 0; i < displayTexts.Length; i++)
           {
               if (displayTexts[i] == null || i >= displayWords.Length)
                   continue;

               string targetWord = displayWords[i];
               if (letterIndex >= targetWord.Length)
                   continue;

               for (int s = 0; s < randomSteps; s++)
               {
                   displayedChars[i][letterIndex] = (char)('A' + Rnd.Range(0, 26));
               }

               // Final correct letter
               displayedChars[i][letterIndex] = targetWord[letterIndex];
               displayTexts[i].text = new string(displayedChars[i]);
           }

           yield return new WaitForSeconds(flipDelay);
       }
   }

   void Solve () {
      StartCoroutine(AnimateSolveText()); // start animation
		  Audio.PlaySoundAtTransform("Solve", displayTexts[0].transform);
      moduleBG.material = moduleMats[2];
      GetComponent<KMBombModule>().HandlePass();
			ModuleSolved = true;
   }

   private IEnumerator strikeBackground(){
    float waitTime = 0.2f;
    moduleBG.material = moduleMats[1];
    yield return new WaitForSeconds(waitTime);
    moduleBG.material = moduleMats[0];
   }

   void Strike () {
     StartCoroutine(strikeBackground()); // strike red
		  Audio.PlaySoundAtTransform("Strike", displayTexts[0].transform);
      GetComponent<KMBombModule>().HandleStrike();
      //well, OKAY... since you asked so politely
      StartCoroutine(AlarmForCurrentNumber());
   }

	 //Twitch Plays code
	 //Did rudimentary testing in TestHarness and seems to work with both individual buttons and sequenced (11/6/2025)
	 #pragma warning disable 414
	 private readonly string TwitchHelpMessage = @"“!{0} press [A-D]” to press that respective button sequence, e.g., !press DDBAC";
	 #pragma warning restore 414

	 IEnumerator ProcessTwitchCommand(string command)
	 {
	     command = command.Trim().ToUpper();
	     yield return null;

	     if (!command.StartsWith("PRESS "))
	         yield break;

			 //grabs sequence post PRESS
	     string sequence = command.Substring(6);

	     foreach (char letter in sequence)
	     {
	         switch (letter)
	         {
	             case 'A':
	                 Buttons[0].OnInteract();
	                 break;
	             case 'B':
	                 Buttons[1].OnInteract();
	                 break;
	             case 'C':
	                 Buttons[2].OnInteract();
	                 break;
	             case 'D':
	                 Buttons[3].OnInteract();
	                 break;
	             default:
	                 Debug.LogWarningFormat("[Jumble #{0}] Unknown button in sequence: {1}", ModuleId, letter);
	                 break;
	         }
	         yield return new WaitForSeconds(0.1f);
	     }
	 }

	 IEnumerator TwitchHandleForcedSolve()
	 {
	     yield return null;
	     if (activeSequence == null)
	         yield break;
	     foreach (string letter in activeSequence)
	     {
	         switch (letter)
	         {
	             case "A":
	                 Buttons[0].OnInteract();
	                 break;
	             case "B":
	                 Buttons[1].OnInteract();
	                 break;
	             case "C":
	                 Buttons[2].OnInteract();
	                 break;
	             case "D":
	                 Buttons[3].OnInteract();
	                 break;
	             default:
	                 Debug.LogFormat("[Jumble #{0}] Unknown button in sequence: {1}", ModuleId, letter);
	                 break;
	         }
	         yield return new WaitForSeconds(0.1f);
	     }
	 }
}

//Please submit bug reports to ME (cameronos on Discord).
//BPI, MTV, BBC, Please them!
