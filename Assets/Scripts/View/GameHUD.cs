using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using ChessBattle.Core;

namespace ChessBattle.View
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Status")]
        public TextMeshProUGUI TurnText;
        public TextMeshProUGUI CheckText;
        
        [Header("Player Panels")]
        public GameObject WhitePanel;
        public GameObject BlackPanel;
        public TextMeshProUGUI WhiteNameText;
        public TextMeshProUGUI BlackNameText;
        public UnityEngine.UI.Image WhiteLogo;
        public UnityEngine.UI.Image BlackLogo;
        
        [Header("Thoughts")]
        public GameObject WhiteBubble;
        public TextMeshProUGUI WhiteBubbleText;
        public GameObject BlackBubble;
        public TextMeshProUGUI BlackBubbleText;

        private void Start()
        {
            if (CheckText) CheckText.gameObject.SetActive(false);
            HideBubbles();
        }

        public void SetLogos(Sprite whiteSprite, Sprite blackSprite)
        {
            if (WhiteLogo && whiteSprite) WhiteLogo.sprite = whiteSprite;
            if (BlackLogo && blackSprite) BlackLogo.sprite = blackSprite;
        }

        public void SetTurn(TeamColor turn)
        {
            if (TurnText) TurnText.text = $"{turn}'s Turn";
            
            // Highlight active panel
            if (WhitePanel && BlackPanel)
            {
                float activeScale = 1.1f;
                float inactiveScale = 1.0f;
                
                if (turn == TeamColor.White)
                {
                    WhitePanel.transform.DOScale(activeScale, 0.3f);
                    BlackPanel.transform.DOScale(inactiveScale, 0.3f);
                }
                else
                {
                    WhitePanel.transform.DOScale(inactiveScale, 0.3f);
                    BlackPanel.transform.DOScale(activeScale, 0.3f);
                }
            }
        }

        public void ShowCheck(bool isCheck)
        {
            if (!CheckText) return;
            
            CheckText.gameObject.SetActive(isCheck);
            if (isCheck)
            {
                CheckText.transform.localScale = Vector3.zero;
                CheckText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBounce);
            }
        }

        public void ShowThought(TeamColor team, string text)
        {
            GameObject bubble = (team == TeamColor.White) ? WhiteBubble : BlackBubble;
            TextMeshProUGUI txt = (team == TeamColor.White) ? WhiteBubbleText : BlackBubbleText;

            if (bubble && txt)
            {
                bubble.SetActive(true);
                txt.text = text;
                
                // Pop animation
                bubble.transform.localScale = Vector3.zero;
                bubble.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

                // Auto hide after some time
                StopAllCoroutines();
                StartCoroutine(HideBubbleRoutine(bubble, 4.0f));
            }
        }

        private IEnumerator HideBubbleRoutine(GameObject bubble, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (bubble)
            {
                bubble.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => 
                {
                    bubble.SetActive(false);
                });
            }
        }

        private void HideBubbles()
        {
            if (WhiteBubble) WhiteBubble.SetActive(false);
            if (BlackBubble) BlackBubble.SetActive(false);
        }
    }
}
