using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mitchel.DialogueSystem
{
    public class DialogueTransitions : MonoBehaviour
    {
        public static event Action SpriteFadeInFinish;
        public static event Action SpriteFadeOutFinish;
        
        [Header("Transition In Effect Settings")]
        [SerializeField] private AnimationCurve panelSlideInCurve;
        [SerializeField] private float panelSlideInAmount;
        [SerializeField] private float panelFadeInTime;
        [Space(5)]
        [SerializeField] private AnimationCurve spriteSlideInCurve;
        [SerializeField] private float npcSpriteInDelay;
        [SerializeField] private float spriteSlideInAmount;
        [SerializeField] private float spriteFadeInTime;
        [Space(5)] 
        [SerializeField] private AnimationCurve headerPanelSlideInCurve;
        [SerializeField] private float headerPanelSlideInAmount;

        [Header("Transition Out Effect Settings")] 
        [SerializeField] private float panelFadeOutTime;
        [Space(5)]
        [SerializeField] private float npcSpriteOutDelay;
        [SerializeField] private float spriteFadeOutTime;

        [Header("Character Change Transition Settings")] 
        [SerializeField] private float characterFadeOutTime;
        [Space(5)]
        private float rightHorizontalPanelTransform;
        [SerializeField] private float leftHorizontalPanelShift;
        private float rightHorizontalCharacterSpriteTransform;
        [SerializeField] private float leftHorizontalCharacterSpriteShift;
        private float rightHorizontalHeaderPanelTransform;
        [SerializeField] private float leftHorizontalHeaderPanelShift;
        private float rightHorizontalTextTransform;
        [SerializeField] private float leftHorizontalTextShift;
        private float rightHorizontalPromptTransform;
        [SerializeField] private float leftHorizontalPromptShift;

        [Header("Object References")] 
        [SerializeField] private RectTransform dialoguePanel;
        [SerializeField] private RectTransform dialogueHeaderPanel;
        [SerializeField] private TextMeshProUGUI dialogueHeaderText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image primarySprite;
        [SerializeField] private Image promptImage;
        
        [HideInInspector] public bool MainCharacterSide = false;
        [HideInInspector] public bool ReadyToProceed = false;
        [HideInInspector] public Sprite QueuedSprite;
        [HideInInspector] public Color CurrentColour;
        [HideInInspector] public Color CurrentHeaderColour;
        private bool dialogueActive;
        private Color opaquePanelColour;
        private Color transparentPanelColour;
        private Color opaqueTestSpriteColour;
        private Color transparentTestSpriteColour;
        private Color opaqueTextColour;
        private Color transparentTextColour;
        private Color opaqueHeaderPanelColour;
        private Color transparentHeaderPanelColour;
        
        // =========== Private object reference variables ===========
        private DialogueSystem dialogueSys;
        private Image dialoguePanelImage;
        private Image dialogueHeaderPanelImage;

        private void Start()
        {
            // Object reference assignment
            dialogueSys = GetComponent<DialogueSystem>();
            dialoguePanelImage = dialoguePanel.gameObject.GetComponent<Image>();
            dialogueHeaderPanelImage = dialogueHeaderPanel.gameObject.GetComponent<Image>();
            
            // Colour assignment
            opaqueTestSpriteColour = primarySprite.color;
            transparentTestSpriteColour = 
                new Color(primarySprite.color.r, primarySprite.color.g, primarySprite.color.b, 0);
            opaqueTextColour = dialogueHeaderText.color;
            transparentTextColour =
                new Color(dialogueHeaderText.color.r, dialogueHeaderText.color.g, dialogueHeaderText.color.b, 0);

            rightHorizontalPanelTransform = dialoguePanel.localPosition.x;
            rightHorizontalCharacterSpriteTransform = primarySprite.rectTransform.localPosition.x;
            rightHorizontalHeaderPanelTransform = dialogueHeaderPanel.localPosition.x;
            rightHorizontalTextTransform = dialogueText.rectTransform.localPosition.x;
            rightHorizontalPromptTransform = promptImage.rectTransform.localPosition.x;
        }
        
        public void EnterDialogue()
        {
            dialoguePanel.gameObject.SetActive(true);
            StartCoroutine(BeginPanelTransitionIn());
            StartCoroutine(BeginCharacterTransitionIn());
        }

        public void ExitDialogue()
        {
            if (ReadyToProceed)
            {
                StartCoroutine(BeginPanelTransitionOut());
                StartCoroutine(BeginCharacterTransitionOut());
            }
        }

        public void ChangeCharacter()
        {
            StartCoroutine(ChangeCharacterTransition());
        }

        private IEnumerator BeginPanelTransitionIn()
        {
            dialogueActive = true;
            ReadyToProceed = false;
            float timeElapsed = 0;
            float endTime = panelSlideInCurve.keys[^1].time;

            // Initialise all the fade in stuff
            opaquePanelColour = CurrentColour;
            transparentPanelColour = new Color(CurrentColour.r, CurrentColour.g, CurrentColour.b, 0);
            opaqueHeaderPanelColour = CurrentHeaderColour;
            transparentHeaderPanelColour = 
                new Color(CurrentHeaderColour.r, CurrentHeaderColour.g, 
                    CurrentHeaderColour.b, 0);
            dialoguePanelImage.color = transparentPanelColour;

            // Initialise all the slide in stuff
            Vector3 oldPanelPos;
            Vector3 newPanelPos;
            float panelShift = rightHorizontalPanelTransform - leftHorizontalPanelShift;
            float textShift = rightHorizontalTextTransform - leftHorizontalTextShift;
            float promptShift = rightHorizontalPromptTransform - leftHorizontalPromptShift;
            if (MainCharacterSide)
            {
                // Set the slide in values for the left-side panel
                oldPanelPos = new Vector3(panelShift - panelSlideInAmount,
                    dialoguePanel.localPosition.y, dialoguePanel.localPosition.z);
                newPanelPos = new Vector3(panelShift,
                    dialoguePanel.localPosition.y, dialoguePanel.localPosition.z);
                
                // Set the left-side position for the text
                var textPos = dialogueText.transform.localPosition;
                textPos.x = textShift;
                dialogueText.transform.localPosition = textPos;
                
                // Set the left-side position for the prompt image
                var promptPos = promptImage.transform.localPosition;
                promptPos.x = promptShift;
                promptImage.transform.localPosition = promptPos;
            }
            else
            {
                // Set the slide in values for the right-side panel
                oldPanelPos = new Vector3(rightHorizontalPanelTransform + panelSlideInAmount,
                    dialoguePanel.localPosition.y, dialoguePanel.localPosition.z);
                newPanelPos = new Vector3(rightHorizontalPanelTransform,
                    dialoguePanel.localPosition.y, dialoguePanel.localPosition.z);
                
                // Set the right-side position for the text (in case it isn't already set)
                var textPos = dialogueText.transform.localPosition;
                textPos.x = rightHorizontalTextTransform;
                dialogueText.transform.localPosition = textPos;
                
                // Set the right-side position for the prompt image
                var promptPos = promptImage.transform.localPosition;
                promptPos.x = rightHorizontalPromptTransform;
                promptImage.transform.localPosition = promptPos;
            }
            dialoguePanel.localPosition = oldPanelPos;
            
            while (timeElapsed < endTime)
            {
                // Fade effect
                if (timeElapsed < panelFadeInTime)
                {
                    dialoguePanelImage.color = Color.Lerp(transparentPanelColour, opaquePanelColour, timeElapsed / panelFadeInTime);
                }
                
                // Slide in effect
                dialoguePanel.localPosition =
                    Vector3.Lerp(oldPanelPos, newPanelPos, panelSlideInCurve.Evaluate(timeElapsed));
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            dialoguePanelImage.color = opaquePanelColour;
            dialoguePanel.localPosition = newPanelPos;
            //Debug.Log("Panel transition in is finished.");
        }

        private IEnumerator BeginCharacterTransitionIn()
        {
            // General initialisation of variables
            float timeElapsed = 0;
            float endTime = spriteSlideInCurve.keys[^1].time;
            var spriteTransform = primarySprite.GetComponent<RectTransform>();
            primarySprite.sprite = QueuedSprite;

            // Initialising all the slide in stuff
            Vector3 oldSpritePos;
            Vector3 newSpritePos;
            Vector3 oldHeaderPanelPos;
            Vector3 newHeaderPanelPos;
            float spriteShift = rightHorizontalCharacterSpriteTransform - leftHorizontalCharacterSpriteShift;
            float headerPanelShift = rightHorizontalHeaderPanelTransform - leftHorizontalHeaderPanelShift;
            if (MainCharacterSide)
            {
                // Set the slide in values for the right-side sprite
                oldSpritePos = new Vector3(spriteShift - spriteSlideInAmount,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                newSpritePos = new Vector3(spriteShift,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                
                // Set the slide in values for the right-side header panel
                oldHeaderPanelPos = new Vector3(headerPanelShift - headerPanelSlideInAmount,
                    dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
                newHeaderPanelPos = new Vector3(headerPanelShift,
                    dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
            }
            else
            {
                // Set the slide in values for the right-side sprite
                oldSpritePos = new Vector3(rightHorizontalCharacterSpriteTransform + spriteSlideInAmount,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                newSpritePos = new Vector3(rightHorizontalCharacterSpriteTransform,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                
                // Set the slide in values for the right-side header panel
                oldHeaderPanelPos = new Vector3(rightHorizontalHeaderPanelTransform + headerPanelSlideInAmount,
                    dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
                newHeaderPanelPos = new Vector3(rightHorizontalHeaderPanelTransform,
                    dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
            }
            spriteTransform.localPosition = oldSpritePos;
            //dialogueHeaderPanel.localPosition = oldHeaderPanelPos;
            dialogueHeaderPanel.localPosition = new Vector3(0, dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
            
            // Initialising all the fade in stuff
            primarySprite.color = transparentTestSpriteColour;
            dialogueHeaderPanelImage.color = transparentHeaderPanelColour;
            dialogueHeaderText.color = transparentTextColour;

            // The delay between the panel transition and the sprite transition
            yield return new WaitForSeconds(npcSpriteInDelay);
            yield return new WaitUntil(() => primarySprite.sprite != null);

            while (timeElapsed < endTime)
            {
                if (timeElapsed < spriteFadeInTime)
                {
                    primarySprite.color = Color.Lerp(transparentTestSpriteColour, opaqueTestSpriteColour, timeElapsed / spriteFadeInTime);
                    dialogueHeaderPanelImage.color = Color.Lerp(transparentPanelColour, opaquePanelColour,
                        timeElapsed / spriteFadeInTime);
                    dialogueHeaderText.color = Color.Lerp(transparentTextColour, opaqueTextColour,
                        timeElapsed / spriteFadeInTime);
                }

                spriteTransform.localPosition =
                    Vector3.Lerp(oldSpritePos, newSpritePos, spriteSlideInCurve.Evaluate(timeElapsed));
                dialogueHeaderPanel.localPosition = Vector3.Lerp(oldHeaderPanelPos, newHeaderPanelPos,
                    headerPanelSlideInCurve.Evaluate(timeElapsed));
                
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            spriteTransform.localPosition = newSpritePos;
            dialogueHeaderPanel.localPosition = newHeaderPanelPos;
            primarySprite.color = opaqueTestSpriteColour;

            ReadyToProceed = true;
            SpriteFadeInFinish?.Invoke();
            dialogueSys.PrintDialogue();
        }

        private IEnumerator BeginPanelTransitionOut()
        {
            //Debug.Log("Begin panel transition out");
            float timeElapsed = 0;
            
            // Initialise all the colour stuff
            opaquePanelColour = dialoguePanelImage.color;
            transparentPanelColour = new Color(dialoguePanelImage.color.r, dialoguePanelImage.color.g,
                dialoguePanelImage.color.b, 0);
            opaqueHeaderPanelColour = dialogueHeaderPanelImage.color;
            transparentHeaderPanelColour = new Color(dialogueHeaderPanelImage.color.r, dialogueHeaderPanelImage.color.g,
                dialogueHeaderPanelImage.color.b, 0);

            while (timeElapsed < panelFadeOutTime)
            {
                dialoguePanelImage.color = Color.Lerp(opaquePanelColour, transparentPanelColour,
                    timeElapsed / panelFadeOutTime);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            
            dialoguePanelImage.color = transparentPanelColour;
            //Debug.Log("Panel transition out is done.");
        }

        private IEnumerator BeginCharacterTransitionOut()
        {
            //Debug.Log("Begin sprite transition out");
            float timeElapsed = 0;
            dialogueHeaderText.text = "";

            yield return new WaitForSeconds(npcSpriteOutDelay);
            
            // Initialise all the colour stuff
            opaqueHeaderPanelColour = dialogueHeaderPanelImage.color;
            transparentHeaderPanelColour = new Color(dialogueHeaderPanelImage.color.r, dialogueHeaderPanelImage.color.g,
                dialogueHeaderPanelImage.color.b, 0);
            
            while (timeElapsed < spriteFadeOutTime)
            {
                primarySprite.color = Color.Lerp(opaqueTestSpriteColour, transparentTestSpriteColour,
                    timeElapsed / panelFadeOutTime);
                dialogueHeaderPanelImage.color = Color.Lerp(opaqueHeaderPanelColour, transparentHeaderPanelColour,
                    timeElapsed / panelFadeOutTime);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            primarySprite.color = transparentTestSpriteColour;
            dialoguePanel.gameObject.SetActive(false);
            primarySprite.sprite = null;
            SpriteFadeOutFinish?.Invoke();
            dialogueActive = false;
            //Debug.Log("Sprite transition out is done.");
        }

        // TODO: When the time is available, make this possible with the BeginSpriteTransitionIn and BeginSpriteTransitionOut coroutines themselves.
        // TODO: When the time is available, separate the panel switch transition into its own separate coroutine so it can run independent of character transition time.
        private IEnumerator ChangeCharacterTransition()
        {
            // General initialisation of variables
            ReadyToProceed = false;
            DialogueSystem.Instance.GoodToGo = false;
            float timeElapsed = 0;
            float endTime = spriteSlideInCurve.keys[^1].time;

            // Initialising the colour fade in stuff
            Color oldPanelColour = dialoguePanelImage.color;
            Color newPanelColour = CurrentColour;
            Color oldPanelHeaderColour = dialogueHeaderPanelImage.color;
            Color newPanelHeaderColour = CurrentHeaderColour;

            while (timeElapsed < characterFadeOutTime)
            {
                primarySprite.color = Color.Lerp(opaqueTestSpriteColour, transparentTestSpriteColour, timeElapsed / characterFadeOutTime);
                //Debug.Log($"Fade out timeElapsed: {timeElapsed}");
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            primarySprite.color = transparentTestSpriteColour;
            primarySprite.sprite = QueuedSprite;
            
            // Initialising the slide in stuff
            RectTransform spriteTransform = primarySprite.GetComponent<RectTransform>();
            Vector3 oldSpritePos;
            Vector3 newSpritePos;
            Vector3 oldPanelPos;
            Vector3 newPanelPos;
            Vector3 oldHeaderPanelPos;
            Vector3 newHeaderPanelPos;
            float spriteShift = rightHorizontalCharacterSpriteTransform - leftHorizontalCharacterSpriteShift;
            float panelShift = rightHorizontalPanelTransform - leftHorizontalPanelShift;
            float headerPanelShift = rightHorizontalHeaderPanelTransform - leftHorizontalHeaderPanelShift;
            float textShift = rightHorizontalTextTransform - leftHorizontalTextShift;
            float promptShift = rightHorizontalPromptTransform - leftHorizontalPromptShift;
            if (MainCharacterSide)
            {
                // Set the slide in values for the left-side sprite
                oldSpritePos = new Vector3(spriteShift - spriteSlideInAmount,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                newSpritePos = new Vector3(spriteShift,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                
                // Set the slide in values for the left-side panel
                oldPanelPos = dialoguePanel.localPosition;
                newPanelPos = new Vector3(panelShift, dialoguePanel.localPosition.y,
                    dialoguePanel.localPosition.z);

                // Set the slide in values for the left-side header panel
                oldHeaderPanelPos = dialogueHeaderPanel.localPosition;
                newHeaderPanelPos = new Vector3(headerPanelShift,
                    dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
                
                // Set the right-side position for the text (in case it isn't already set)
                var textPos = dialogueText.transform.localPosition;
                textPos.x = textShift;
                dialogueText.transform.localPosition = textPos;
                
                // Set the right-side position for the prompt image
                var promptPos = promptImage.transform.localPosition;
                promptPos.x = promptShift; 
                promptImage.transform.localPosition = promptPos;
            }
            else
            {
                // Set the slide in values for the right-side sprite
                oldSpritePos = new Vector3(rightHorizontalCharacterSpriteTransform + spriteSlideInAmount,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                newSpritePos = new Vector3(rightHorizontalCharacterSpriteTransform,
                    spriteTransform.localPosition.y, spriteTransform.localPosition.z);
                
                // Set the slide in values for the right panel
                oldPanelPos = dialoguePanel.localPosition;
                newPanelPos = new Vector3(rightHorizontalPanelTransform, dialoguePanel.localPosition.y,
                    dialoguePanel.localPosition.z);

                // Set the slide in values for the left-side header panel
                oldHeaderPanelPos = dialogueHeaderPanel.localPosition;
                newHeaderPanelPos = new Vector3(rightHorizontalHeaderPanelTransform,
                    dialogueHeaderPanel.localPosition.y, dialogueHeaderPanel.localPosition.z);
                
                // Set the left-side position for the text
                var textPos = dialogueText.transform.localPosition;
                textPos.x = rightHorizontalTextTransform;
                dialogueText.transform.localPosition = textPos;
                
                // Set the left-side position for the prompt image
                var promptPos = promptImage.transform.localPosition;
                promptPos.x = rightHorizontalPromptTransform;
                promptImage.transform.localPosition = promptPos;
            }
            
            spriteTransform.localPosition = oldSpritePos;
            timeElapsed = 0;
            float panelEndTime = panelSlideInCurve.keys[panelSlideInCurve.length - 1].time;
            
            while (timeElapsed < endTime)
            {
                if (timeElapsed < panelEndTime)
                {
                    dialoguePanel.localPosition = Vector3.Lerp(oldPanelPos, newPanelPos, panelSlideInCurve.Evaluate(timeElapsed));
                    dialogueHeaderPanel.localPosition = Vector3.Lerp(oldHeaderPanelPos, newHeaderPanelPos,
                        panelSlideInCurve.Evaluate(timeElapsed));
                }
                spriteTransform.localPosition =
                    Vector3.Lerp(oldSpritePos, newSpritePos, spriteSlideInCurve.Evaluate(timeElapsed));
                primarySprite.color = Color.Lerp(transparentTestSpriteColour, opaqueTestSpriteColour,
                    timeElapsed / spriteFadeInTime);
                dialoguePanelImage.color = Color.Lerp(oldPanelColour, CurrentColour, timeElapsed / spriteFadeInTime);
                dialogueHeaderPanelImage.color = Color.Lerp(oldPanelHeaderColour, newPanelHeaderColour, timeElapsed / spriteFadeInTime);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            primarySprite.color = opaqueTestSpriteColour;
            dialoguePanelImage.color = newPanelColour;
            dialogueHeaderPanelImage.color = newPanelHeaderColour;
            spriteTransform.localPosition = newSpritePos;
            dialoguePanel.localPosition = newPanelPos;
            dialogueHeaderPanel.localPosition = newHeaderPanelPos;

            ReadyToProceed = true;
            dialogueSys.GoodToGo = true;
        }

        /// <summary>
        /// Is the dialogue system currently running?
        /// </summary>
        /// <returns></returns>
        public bool IsDialogueActive()
        {
            return dialogueActive;
        }
    }
}