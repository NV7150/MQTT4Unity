# MQTT4Unity

UnityでMQTT通信を簡単に扱うためのラッパーです。  
[MQTTnet](https://github.com/dotnet/MQTTnet)をベースに、Unity向けにコールバックやワイルドカードトピック対応などを提供します。

---

## 特徴

- MQTTnetベースの高機能・高互換性
- ワイルドカード（`+`, `#`）トピックサブスクライブ対応
- Unityのメインスレッドでコールバック実行
- 認証（ユーザー名/パスワード、ClientId）対応
- サンプルスクリプト付き

---

## インストール

1. リリースにあるunitypackageをプロジェクトにインポートしてください。

---

## 使い方

### 1. MqttComManagerのセットアップ

- 空のGameObjectを作成し、`MqttComManager`コンポーネントをアタッチします。
- InspectorでBrokerのドメイン、ポート、認証情報などを設定します。

#### 1-ex. CA証明書を組み込む
CA証明書を用いて，MQTTサーバを検証してTLS接続できるようになりました．
1. **CA証明書(.certファイル)の拡張子を.txtにする**(こうしないとInspectorで指定できない)
1. MqttComManagerのEnable Ca Server Authにチェックを入れる
1. 拡張子を変えたCA証明書ファイルをUnityに入れ，Ca Cert Fileにいれる

あとは認証情報があっていれば，TLS接続に変更されるはず
注：MQTTサーバのオレオレ証明書を使っているのを想定しているので，常にRevocationMode = Falseにしてます

### 2. サブスクライブ（購読）

```csharp
using MQTT4Unity;

public class MqttSubscribeSample : MonoBehaviour {
    [SerializeField] private MqttComManager mqttMan;
    [SerializeField] private string mqttTopic;

    void Start() {
        mqttMan.Subscribe(mqttTopic, MsgArrived);
    }

    void MsgArrived(string msg) {
        Debug.Log("受信: " + msg);
    }
}
```

### 3. パブリッシュ（送信）

```csharp
using MQTT4Unity;

public class MqttPublishSample : MonoBehaviour {
    [SerializeField] private MqttComManager mqttMan;
    [SerializeField] private string mqttTopic;

    public void PublishTest(string msg) {
        mqttMan.Publish(mqttTopic, msg);
    }
}
```

### 4. 接続状態の取得・ログ

```csharp
void Update() {
    if (mqttMan.IsConnected) {
        // 接続中
    }
}
mqttMan.OnLog += msg => Debug.Log(msg);
```

---

## サンプル

`Assets/MQTT4Unity/Scripts/SampleScript`にサンプルスクリプトが含まれています。

- `MqttSubscribeSample.cs` … メッセージ購読例
- `MqttPublishSample.cs` … メッセージ送信例
- `MqttStatus.cs` … 接続状態・ログ表示例

---

## ライセンス

MIT

---

# MQTT4Unity

A wrapper for easy MQTT communication in Unity.  
Based on [MQTTnet](https://github.com/dotnet/MQTTnet), it provides Unity-friendly callbacks and wildcard topic support.

---

## Features

- High compatibility and functionality based on MQTTnet
- Wildcard topic subscribe support (`+`, `#`)
- Callbacks run on Unity main thread
- Authentication (username/password, ClientId) support
- Includes sample scripts

---

## Installation

1. Import the unitypackage from the Releases page into your project.

---

## Usage

### 1. Setup MqttComManager

- Create an empty GameObject and attach the `MqttComManager` component.
- Set the broker domain, port, and authentication info in the Inspector.

### 2. Subscribe

```csharp
using MQTT4Unity;

public class MqttSubscribeSample : MonoBehaviour {
    [SerializeField] private MqttComManager mqttMan;
    [SerializeField] private string mqttTopic;

    void Start() {
        mqttMan.Subscribe(mqttTopic, MsgArrived);
    }

    void MsgArrived(string msg) {
        Debug.Log("Received: " + msg);
    }
}
```

### 3. Publish

```csharp
using MQTT4Unity;

public class MqttPublishSample : MonoBehaviour {
    [SerializeField] private MqttComManager mqttMan;
    [SerializeField] private string mqttTopic;

    public void PublishTest(string msg) {
        mqttMan.Publish(mqttTopic, msg);
    }
}
```

### 4. Connection Status & Logging

```csharp
void Update() {
    if (mqttMan.IsConnected) {
        // Connected
    }
}
mqttMan.OnLog += msg => Debug.Log(msg);
```

---

## Samples

Sample scripts are included in `Assets/MQTT4Unity/Scripts/SampleScript`:

- `MqttSubscribeSample.cs` … Example of subscribing
- `MqttPublishSample.cs` … Example of publishing
- `MqttStatus.cs` … Example of connection status and log display

---

## License

MIT 