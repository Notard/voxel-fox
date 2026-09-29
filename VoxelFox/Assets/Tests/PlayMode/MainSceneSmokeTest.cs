using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

// Main 씬을 Play 모드로 로드해 몇 프레임 돌려도 에러 로그가 없는지 확인한다.
// 에러/예외 로그가 나오면 Unity Test Framework가 테스트를 실패 처리한다.
public class MainSceneSmokeTest
{
    [UnityTest]
    public IEnumerator MainScene_RunsWithoutErrors()
    {
        yield return SceneManager.LoadSceneAsync("Main");
        Assert.AreEqual("Main", SceneManager.GetActiveScene().name);

        for (int i = 0; i < 60; i++)
            yield return null;

        Assert.IsNotNull(Camera.main, "Main Camera가 없음");
    }
}
