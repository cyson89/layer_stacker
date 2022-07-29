using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class LayerStackerManager : MonoBehaviour
{  
    public GameObject defaultLayerFormat;
    public GameObject scrollViewContent;

    public GameObject canvasPreview;
    public GameObject rawImagePreview;

    public GameObject textAddLayer;

    public TMP_InputField inputFieldOutputPath;

    public TMP_Text textLogMessage;

    // Start is called before the first frame update
    void Start()
    {
        defaultLayerFormat.SetActive(false);
        defaultForbiddenFormat.SetActive(false);
        defaultDependencyFormat.SetActive(false);
    }


    #region

    private GameObject currentSelectedInputField;
    public void SetFolderPath()
    {
        currentSelectedInputField = EventSystem.current.currentSelectedGameObject;       

        StartCoroutine(CoSetFolderPath());
    }

    IEnumerator CoSetFolderPath()
    {      
        PopupFileBrowser();     

        yield return new WaitForSeconds(0.01f);
    }

    private string filePathSelected;
    public void PopupFileBrowser()
    {        
        var paths = StandaloneFileBrowser.OpenFolderPanel("Folder Path", "" ,false);

        GameObject g;

        g = EventSystem.current.currentSelectedGameObject;

        g.GetComponent<TMP_InputField>().text = paths[0];

        Debug.Log(paths[0].Split("\\").Length);

        g.transform.parent.Find("Text (TMP)_FolderName").GetComponent<TMP_Text>().text = paths[0].Split("\\")[paths[0].Split("\\").Length - 1];

        filePathSelected = paths[0];

        OnDeselected();
    }

    public static long CountDirectoryFile(DirectoryInfo d)
    {
        long i = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            if (fi.Extension.Contains("png") || fi.Extension.Contains("PNG"))
                i++;
        }
        return i;
    }



    public void OnDeselected()
    {
        Debug.Log("Analize Folder...");

        GameObject g = EventSystem.current.currentSelectedGameObject;

        DirectoryInfo d = new DirectoryInfo(filePathSelected);

        Debug.Log(CountDirectoryFile(d));

        Debug.Log(g.name);

        g.transform.parent.Find("Text (TMP)_NumOfImage").GetComponent<TMP_Text>().text = CountDirectoryFile(d).ToString();


        FileInfo[] fis = d.GetFiles();

        byte[] fileData;

        Debug.Log(filePathSelected + "/" + fis[0].Name);

        fileData = File.ReadAllBytes(filePathSelected + "/" + fis[0].Name);

        Texture2D tex = null;

        tex = new Texture2D(2, 2);

        tex.LoadImage(fileData);

        g.transform.parent.GetComponent<LayerConnector>().connectedLayer.GetComponent<RawImage>().texture = tex;

        g.transform.parent.GetComponent<LayerConnector>().connectedLayer.GetComponent<RawImage>().enabled = true;

        g.transform.parent.GetComponent<LayerConnector>().connectedLayer.GetComponent<LayerInformation>().imagesDir.Clear();

        for (int i = 0; i < CountDirectoryFile(d); i++)
        {
            g.transform.parent.GetComponent<LayerConnector>().connectedLayer.GetComponent<LayerInformation>().imagesDir.Add(fis[i].ToString());
        }
       
    }

    private GameObject selectedButton;
    public void RemoveLayer()
    {
        selectedButton = EventSystem.current.currentSelectedGameObject;

        addedLayerList.Remove(selectedButton.transform.parent.gameObject);

        Destroy(selectedButton.transform.parent.gameObject);

        Invoke(nameof(RefreshLayerOrderNumber), 0.2f);
        
    }

    private void RefreshLayerOrderNumber()
    {

        Debug.Log(scrollViewContent.transform.childCount - 1);
        for (int i = 0; i < scrollViewContent.transform.childCount ; i++)
        {
            if(i >= 0)
            {
                Debug.Log(scrollViewContent.transform.GetChild(i).name);
                Debug.Log("index: "+i);
                scrollViewContent.transform.GetChild(i).Find("Text (TMP)_Order").GetComponent<TMP_Text>().text = (i).ToString();

                scrollViewContent.transform.GetChild(i).GetComponent<LayerConnector>().connectedLayer.transform.SetSiblingIndex(i);

            }          
        }

        if (scrollViewContent.transform.childCount == 0)
        {
            textAddLayer.SetActive(true);
        }

    }

    public List<GameObject> addedLayerList = new List<GameObject>(); 
    private GameObject lastAddedLayer;
    private GameObject lastAddedRawImage;
    public void AddLayer()
    {
        textAddLayer.SetActive(false);

        lastAddedLayer = Instantiate(defaultLayerFormat, scrollViewContent.transform);
        lastAddedLayer.SetActive(true);

        addedLayerList.Add(lastAddedLayer);

        lastAddedLayer.transform.Find("Text (TMP)_Order").GetComponent<TMP_Text>().text = (scrollViewContent.transform.childCount - 1).ToString();

            
        lastAddedRawImage = Instantiate(rawImagePreview, canvasPreview.transform);
        lastAddedLayer.GetComponent<LayerConnector>().connectedLayer = lastAddedRawImage;
        lastAddedRawImage.name = lastAddedRawImage.name + "_" + canvasPreview.transform.childCount.ToString();

    }


    public void ChangeOrder(bool isUp)
    {
        GameObject g = EventSystem.current.currentSelectedGameObject;
        Debug.Log(g.transform.parent.GetSiblingIndex());

        int adder;
        if (isUp)
        {
            adder = -1;
        }
        else
        {
            adder = +1;
        }

        g.transform.parent.SetSiblingIndex(g.transform.parent.GetSiblingIndex() + adder);

        RefreshLayerOrderNumber();

    }



    public void StartGenerate()
    {
        //StartCoroutine(CoStartGenerate());
        CoStartGenerate();
    }

    
    int filteredListCount = 0;

    public List<string> layerSourcePath = new List<string>();

    public List<string> dirCaseList = new List<string>();
    public List<string> filenameCaseList = new List<string>();
    public List<string> removedCaseList = new List<string>();
    public List<List<string>> imagesDirList = new List<List<string>>();
    public List<RawImage> rawImageLayerList = new List<RawImage>();

    int numRemoved = 0; 


    int numOfTotalCase = 1;

    public GameObject canvasToRender;

    string stringOutputPath;


    public object populationLock = new object();
    public object populationLock2 = new object();
    void CoStartGenerate()
    {
        stringOutputPath = inputFieldOutputPath.text;

        dirCaseList.Clear();
        filenameCaseList.Clear();
        removedCaseList.Clear();
        imagesDirList.Clear();
        rawImageLayerList.Clear();

     

        numRemoved = 0;

        fileCounter = 0;

        for (int i = 0; i < canvasToRender.transform.childCount; i++)
        {
            imagesDirList.Add(canvasToRender.transform.GetChild(i).GetComponent<LayerInformation>().imagesDir);
            canvasToRender.transform.GetChild(i).GetComponent<RawImage>().texture = null;
            rawImageLayerList.Add(canvasToRender.transform.GetChild(i).GetComponent<RawImage>());
        }

        //yield return new WaitForSeconds(1);

        Debug.Log(imagesDirList.Count);

        numOfTotalCase = 1;

        for (int i = 0; i < imagesDirList.Count; i++)
        {
            numOfTotalCase *= imagesDirList[i].Count;                        
        }

        for (int i = 0; i < numOfTotalCase; i++)
        {
            dirCaseList.Add("@@");
        }       

        Debug.Log(numOfTotalCase);
        textLogMessage.text = "[" + numOfTotalCase.ToString() + "]" + " Total cases are generated";


        // make all possible case list from source
        int numOfLayer = canvasPreview.transform.childCount;
        int numTotalCase = 0;
        int divider = 1;
        int repeatCounter = 0;
        int counter = 0;
        int numToRepeat = 0;


        for (int i = 0; i < imagesDirList.Count; i++)
        {
            counter = 0;
            repeatCounter = 0;
            divider *= imagesDirList[i].Count;


            for (int j = 0; j < numOfTotalCase; j++)
            {
                numTotalCase++;

                numToRepeat = (numOfTotalCase / divider) - 1;

                dirCaseList[j] = dirCaseList[j] + imagesDirList[i][repeatCounter] + "@@";

                if (counter == numToRepeat)
                {
                    repeatCounter++;

                    if (repeatCounter == imagesDirList[i].Count)
                    {
                        repeatCounter = 0;
                    }
                    counter = 0;
                }
                else
                {
                    counter++;
                }

                //textLogMessage.text = (numTotalCase / numOfLayer).ToString();
            }

        }


        // trim 
        Parallel.For(0, dirCaseList.Count, (i) =>
        {
            dirCaseList[i] = dirCaseList[i].Substring(2, dirCaseList[i].Length - 4);
        });


        layerSourcePath.Clear();
        for (int i = 0; i < addedLayerList.Count; i++)
        {
            layerSourcePath.Add(addedLayerList[i].transform.Find("InputField (TMP)_LayerSourcePath").GetComponent<TMP_InputField>().text);
        }

        for (int i = 0; i < dirCaseList.Count; i++)
        {
            filenameCaseList.Add(dirCaseList[i]);
        }
        // TODO : remove each layer path
        Parallel.For(0, dirCaseList.Count, (i) =>
        {
            for (int j = 0; j < layerSourcePath.Count; j++)
            {
                filenameCaseList[i] = filenameCaseList[i].Replace(layerSourcePath[j] + "\\", "").Replace(".png", "");              
            }         
        });    
    }


    public void ApplyRule()
    {
        //StartCoroutine(CoApplyRule());


        //////////////
        for (int i = 0; i < scrollViewContentDependency.transform.childCount; i++)
        {
            string s = "";
            s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetA").GetComponent<TMP_InputField>().text;

            s += "@@";
            s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetB").GetComponent<TMP_InputField>().text;

            if (scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetC").GetComponent<TMP_InputField>().text != "")
            {
                s += "@@";
                s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetC").GetComponent<TMP_InputField>().text;
            }

            if (scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetD").GetComponent<TMP_InputField>().text != "")
            {
                s += "@@";
                s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetD").GetComponent<TMP_InputField>().text;
            }

            if (scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetE").GetComponent<TMP_InputField>().text != "")
            {
                s += "@@";
                s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetE").GetComponent<TMP_InputField>().text;
            }

            if (scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetF").GetComponent<TMP_InputField>().text != "")
            {
                s += "@@";
                s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetF").GetComponent<TMP_InputField>().text;
            }

            if (scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetG").GetComponent<TMP_InputField>().text != "")
            {
                s += "@@";
                s += scrollViewContentDependency.transform.GetChild(i).Find("InputField (TMP)_SetG").GetComponent<TMP_InputField>().text;
            }

            dependencyRuleList[i] = s;

        }

        //for (int i = 0; i < 10; i++)
        //{
        //    new Thread(CoApplyRule2).Start();
        //}
        CoApplyRule2();
    }

    int numForbiddened = 0;
    private int lastAppliedIndexForbiddenRule = -1;
    private int lastAppliedIndexDependencyRule = -1; 
    public void CoApplyRule2()
    {
        // remove impossible case from FORBIDDEN rules
        Parallel.For(0, filenameCaseList.Count, (i) =>
        {
            for (int j = 0; j < forbiddenRuleList.Count; j++)
            {
                if (j > lastAppliedIndexForbiddenRule)
                {

                    lock (populationLock2)
                    {
                        if (filenameCaseList[i].Contains(forbiddenRuleList[j].Split("@@")[0]) && filenameCaseList[i].Contains(forbiddenRuleList[j].Split("@@")[1]))
                        {
                            removedCaseList.Add(filenameCaseList[i]);

                            //filenameCaseList.RemoveAt(i);
                            //dirCaseList.RemoveAt(i);

                            filenameCaseList[i] = "REMOVED";
                            dirCaseList[i] = "REMOVED";


                        }
                    }
              
                }
            }
            Debug.Log(forbiddenRuleList.Count);
        });


        textLogMessage.text = "[" + numForbiddened.ToString() + "]" + " element(s) are forbiddened";


        for (int i = 0; i < forbiddenRuleList.Count; i++)
        {
            scrollViewContentForbidden.transform.GetChild(i).GetComponent<Image>().color = Color.red;
            scrollViewContentForbidden.transform.GetChild(i).Find("Button_Remove").GetComponent<Button>().interactable = false;
        }

        lastAppliedIndexForbiddenRule = forbiddenRuleList.Count - 1;

        Parallel.For(0, filenameCaseList.Count, (i) =>
        {            
                for (int j = 0; j < dependencyRuleList.Count; j++)
                {
                    if (j > lastAppliedIndexDependencyRule)
                    {
                        int len = dependencyRuleList[j].Split("@@").Length;
                        switch (len)
                        {
                            case 2:
                                if (filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[0]))
                                {
                                    // "suit02" only exists with "hel02" and "helnull"
                                    if (!filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[1]))
                                    {
                                        removedCaseList.Add(filenameCaseList[i]);

                                        filenameCaseList[i] = "REMOVED";
                                        dirCaseList[i] = "REMOVED";                                       
                                    }
                                }

                                break;

                            case 3:
                                if (filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[0]))
                                {                                 
                                    if (!filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[1]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[2]))
                                    {
                                        removedCaseList.Add(filenameCaseList[i]);

                                        filenameCaseList[i] = "REMOVED";
                                        dirCaseList[i] = "REMOVED";                                    
                                    }
                                }

                                break;

                            case 4:
                                if (filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[0]))
                                {                                   
                                    if (!filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[1]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[2])
                                        && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[3]))
                                    {
                                        removedCaseList.Add(filenameCaseList[i]);

                                    filenameCaseList[i] = "REMOVED";
                                    dirCaseList[i] = "REMOVED";                                 
                                    }
                                }

                                break;

                            case 5:
                                if (filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[0]))
                                {                                   
                                    if (!filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[1]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[2])
                                        && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[3]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[4]))
                                    {
                                        removedCaseList.Add(filenameCaseList[i]);

                                    filenameCaseList[i] = "REMOVED";
                                    dirCaseList[i] = "REMOVED";

                                    }
                                }

                                break;
                            case 6:

                                if (filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[0]))
                                {
                                    if (!filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[1]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[2])
                                        && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[3]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[4])
                                        && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[5]))
                                    {
                                        removedCaseList.Add(filenameCaseList[i]);

                                        filenameCaseList[i] = "REMOVED";
                                        dirCaseList[i] = "REMOVED";

                                    }
                                }

                                break;

                        case 7:
                            if (filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[0]))
                            {
                                if (!filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[1]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[2])
                                    && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[3]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[4])
                                    && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[5]) && !filenameCaseList[i].Contains(dependencyRuleList[j].Split("@@")[6]))
                                {
                                    removedCaseList.Add(filenameCaseList[i]);
                                    filenameCaseList[i] = "REMOVED";
                                    dirCaseList[i] = "REMOVED";
                                }
                            }

                            break;


                            default:
                                break;
                        }
                    }
                }
            
        });

        for (int i = 0; i < dependencyRuleList.Count; i++)
        {
            scrollViewContentDependency.transform.GetChild(i).GetComponent<Image>().color = Color.red;
            scrollViewContentDependency.transform.GetChild(i).Find("Button_Remove").GetComponent<Button>().interactable = false;
        }
        lastAppliedIndexDependencyRule = dependencyRuleList.Count - 1;

        //RemoveRemovedElement();
        StartCoroutine(RemoveRemovedElement());
    }
   
    private IEnumerator RemoveRemovedElement()
    {

        List<string> removedDirCaseList = new List<string>();
        List<string> removedFilenameList = new List<string>();

        int numRemoved = 0;
        for (int i = filenameCaseList.Count - 1; i > -1; i--)
        {
            if (filenameCaseList[i] != "REMOVED")
            {
                removedFilenameList.Add(filenameCaseList[i]);
                removedDirCaseList.Add(dirCaseList[i]);

                numRemoved++;
            }          

            if (i % 1000 == 0)
            {
                textLogMessage.text = numOfTotalCase + " - " + (numOfTotalCase - numRemoved) + " = " + numRemoved;
                yield return null;
            }

        }

        filenameCaseList = removedFilenameList;
        dirCaseList = removedDirCaseList;       

    }

    public void GenerateFile()
    {
        StartCoroutine(CoGenerateFile());
    }

    public int fileCounter = 0;
    IEnumerator CoGenerateFile()
    {
        fileCounter = 0;
        filteredListCount = dirCaseList.Count;

        // generate json metadata file
        Texture2D tex = null;
        byte[] fileData;

        for (int i = 0; i < filenameCaseList.Count; i++)
        {      

            for (int j = 0; j < filenameCaseList[i].Split("@@").Length; j++)
            {
                try
                {
                    fileData = File.ReadAllBytes(dirCaseList[i].Split("@@")[j]);

                    tex = new Texture2D(2, 2);                    

                    tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

                }
                catch (Exception e)
                {
                    Debug.Log(e);
                    throw;
                }

                rawImageLayerList[j].texture = tex;
                
            }            
           
            yield return null;

            if (true)
            {
                string traitString = "";
                for (int k = 0; k < filenameCaseList[i].Split("@@").Length; k++)
                {                    
                    traitString = traitString + "##" + filenameCaseList[i].Split("@@")[k];
                }
                TakeScreenshot(traitString);
            }

            for (int j = 0; j < rawImageLayerList.Count; j++)
            {
                Destroy(rawImageLayerList[j].texture);
            }
        }     
    }

    private bool FilterForbiddenCombination()
    {
        bool isPass = true;

        return isPass;
    }

    public Camera renderCam;
    int num;

    public void TakeScreenshot(string str)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderCam.targetTexture;

        renderCam.Render();

        Texture2D Image = new Texture2D(renderCam.targetTexture.width, renderCam.targetTexture.height);
        
        Image.ReadPixels(new Rect(0, 0, renderCam.targetTexture.width, renderCam.targetTexture.height), 0, 0);
        Image.Apply();
        RenderTexture.active = currentRT;

        var Bytes = Image.EncodeToPNG();
       


        Destroy(Image);

        //File.WriteAllBytes(Application.dataPath + "/StreamingAssets/output/" +  str.Substring(2), Bytes);
        File.WriteAllBytes(stringOutputPath + "/" + str.Substring(2) + ".png", Bytes);  

        fileCounter++;

        textLogMessage.text = fileCounter.ToString() + "/" + filteredListCount.ToString();
    }

    #endregion

    #region

    public GameObject textAddRule;

    public GameObject defaultForbiddenFormat;
    public GameObject scrollViewContentForbidden;

    public List<string> forbiddenRuleList = new List<string>();

    public void AddRule()
    {
        textAddRule.SetActive(false);

        GameObject g = Instantiate(defaultForbiddenFormat, scrollViewContentForbidden.transform);

        g.name = (scrollViewContentForbidden.transform.childCount - 1).ToString();

        g.transform.Find("Text (TMP)_RuleNum").GetComponent<TMP_Text>().text = g.name;

        g.SetActive(true);

        forbiddenRuleList.Add("");

    }

    string concatedAB = "";
    string partA = "";
    string partB = "";
    public void UpdateRuleList(bool isA)
    {   
        if (isA)
        {
            partA = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>().text;
        }
        else
        {
            partB = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>().text;
        }


        concatedAB = partA + "@@" + partB;

        forbiddenRuleList[int.Parse(EventSystem.current.currentSelectedGameObject.transform.parent.name)] = concatedAB;

    }
    public void RefreshRuleOrder()
    {
        GameObject g = EventSystem.current.currentSelectedGameObject.transform.parent.gameObject;

        forbiddenRuleList.RemoveAt(int.Parse(g.name));

        Destroy(g);

        StartCoroutine(CoRefreshRuleOrder());
    }
    IEnumerator CoRefreshRuleOrder()
    {
        yield return new WaitForSeconds(0.05f);

        for (int i = 0; i < scrollViewContentForbidden.transform.childCount; i++)
        {
            scrollViewContentForbidden.transform.GetChild(i).name = i.ToString();
            scrollViewContentForbidden.transform.GetChild(i).Find("Text (TMP)_RuleNum").GetComponent<TMP_Text>().text = i.ToString();
        }

        if (scrollViewContentForbidden.transform.childCount == 0)
        {
            textAddRule.SetActive(true);
        }

    }

    public GameObject textAddRuleDependency;

    public GameObject defaultDependencyFormat;
    public GameObject scrollViewContentDependency;

    public List<string> dependencyRuleList = new List<string>();
    public void AddDependencyRule()
    {
        textAddRuleDependency.SetActive(false);

        GameObject g = Instantiate(defaultDependencyFormat, scrollViewContentDependency.transform);

        g.name = (scrollViewContentDependency.transform.childCount - 1).ToString();

        g.transform.Find("Text (TMP)_RuleNum").GetComponent<TMP_Text>().text = g.name;

        g.SetActive(true);

        dependencyRuleList.Add("");
    }

    string concatedSetABC;
    string setA;
    string setB;
    string setC;
    public void UpdateRuleList(int order)
    {

        GameObject g = EventSystem.current.currentSelectedGameObject;
        switch (order)
        {           
            case 0:                    

                setA = g.GetComponent<TMP_InputField>().text;

                g.transform.parent.Find("InputField (TMP)_SetB").GetComponent<TMP_InputField>().text = "";
                g.transform.parent.Find("InputField (TMP)_SetC").GetComponent<TMP_InputField>().text = "";
                setB = "";                
                setC = "";

                break;
            case 1:
               
                setB = g.GetComponent<TMP_InputField>().text;

                g.transform.parent.Find("InputField (TMP)_SetC").GetComponent<TMP_InputField>().text = "";

                setC = "";

                break;
            case 2:
              
                setC = g.GetComponent<TMP_InputField>().text;

                break;
            
            default:
                break;
        }

        if (setC != "")
        {
            concatedSetABC = setA + "@@" + setB + "@@" + setC;
        }
        else
        {         
            concatedSetABC = setA + "@@" + setB;
        }

        dependencyRuleList[int.Parse(EventSystem.current.currentSelectedGameObject.transform.parent.name)] = concatedSetABC;
        concatedSetABC = "";
    }


    public void RefreshDependencyRuleOrder()
    {
        GameObject g = EventSystem.current.currentSelectedGameObject.transform.parent.gameObject;

        dependencyRuleList.RemoveAt(int.Parse(g.name));

        Destroy(g);

        StartCoroutine(CoRefreshDependencyRuleOrder());
    }
    IEnumerator CoRefreshDependencyRuleOrder()
    {
        yield return new WaitForSeconds(0.05f);

        for (int i = 0; i < scrollViewContentDependency.transform.childCount; i++)
        {
            scrollViewContentDependency.transform.GetChild(i).name = i.ToString();
            scrollViewContentDependency.transform.GetChild(i).Find("Text (TMP)_RuleNum").GetComponent<TMP_Text>().text = i.ToString();
        }

        if (scrollViewContentDependency.transform.childCount == 0)
        {
            textAddRuleDependency.SetActive(true);
        }

    }


    #endregion



    [Serializable]
    public class Trait
    {
        public string description;
      
        public string external_url;
        public string image;
        public string name;
        public Attribute[] attributes;

        //public string badge;
        //public string emblem;
        //public string coin;
        //public string hair;
        //public string mask;
        //public string headwear;
        //public string fullhelmet;
        //public string eyewear;

    }

    [Serializable]
    public struct Attribute
    {
        public string trait_type;
        public string value;
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.T))
        {
            RemoveRemovedElement();
        }

    }
}
