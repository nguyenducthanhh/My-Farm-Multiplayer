using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UsernameWizard : MonoBehaviour
{
    public GameObject usernameWizard;
    public Button buttonOk;
    public InputField inputUsername;

    private FirebaseDatabaseManager databaseManager;
    public Text username;
    public Text gold;
    public Text diamond;
    void Start()
    {
        databaseManager = GameObject.Find("DatabaseManager").GetComponent<FirebaseDatabaseManager>(); // Nên gắn tag để tăng hiệu xuất
        if (LoadDataManager.userInGame.Name == "")
        {
            usernameWizard.SetActive(true);
        }
        else
        {
            usernameWizard.SetActive(false);
            username.text = LoadDataManager.userInGame.Name;
        }
        gold.text = "Gold: " + LoadDataManager.userInGame.Gold.ToString();
        diamond.text = "Diamond: " + LoadDataManager.userInGame.Diamond.ToString();
        buttonOk.onClick.AddListener(SetNewUsername);
    }

    void Update()
    {
        
    }

    public void SetNewUsername()
    {
            LoadDataManager.userInGame.Name = inputUsername.text;

            databaseManager.WriteDatabase("Users/" + LoadDataManager.firebaseUser.UserId, LoadDataManager.userInGame.ToString());
            
            username.text = inputUsername.text;

            usernameWizard.SetActive(false);
        
    }

}
