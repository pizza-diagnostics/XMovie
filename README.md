# XMovie

## これは何？

動画のタグ管理を隠れ蓑にした、C#/WPF/EntityFramework/sqlの学習結果アウトプットです。

## 依存

ffmpeg/ffproveを使用しています。アプリケーション実行ファイルと同じディレクトリにexeファイルが必要です。以下オプションに対応している必要があります。バージョン?いくつだろう??

```c#
// ffproveの引数
var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1 \"{path}\"";
```

```c#
// ffmpegの引数
// seconds: サムネイル作成時間
// moviePath: 動画パス
// thumbnailPath: 出力先
var arg = $"-ss {seconds} -i \"{moviePath}\" -vf scale=160:-1 -f image2 -an -y -vframes 1 \"{thumbnailPath}\"";
```
