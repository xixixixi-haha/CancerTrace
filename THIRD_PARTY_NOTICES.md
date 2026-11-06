# Third-party notices

本仓库没有统一的开源 LICENSE 文件；公开代码不等于获得再分发或商用许可。项目所有者需确定项目代码许可证。本清单不授予任何额外权利。

## AI 生成图片

据项目方确认，清单内 95 项游戏图片均为 OpenAI AI-generated，由 ChatGPT / OpenAI 图像生成功能生成，没有使用从网络下载的第三方图片。生成日期、提示词及适用授权条款未提供。[逐文件来源清单](unity/CancerTraceClient/Assets/Art/ASSET_PROVENANCE.md)

## 第三方字体与音乐

- 思源黑体 Source Han Sans SC Regular 由 Adobe 发布，采用 SIL OFL 1.1。已核对 `SourceHanSansSC-Regular.otf` 内嵌的字体名称、字重及许可证声明。仓库许可证全文包含 Adobe 版权声明，与上游许可证所列版权方及许可证版本一致。字体及 TMP 派生资源适用该字体授权。[仓库许可证全文](unity/Docs/Licenses/SourceHanSans_LICENSE.txt) · [Adobe 上游许可证](https://raw.githubusercontent.com/adobe-fonts/source-han-sans/master/LICENSE.txt)
- Liberation Sans：授权全文位于 `unity/CancerTraceClient/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`。
- BGM 为 Kistol 创作的 **Forget Me Not**，授权为 CC0。游戏使用循环版文件 `forget_me_not_in_f_major_looped.ogg`。OpenGameArt 原始条目发布于 2021-04-03。[OpenGameArt 原始条目](https://opengameart.org/content/forget-me-not) · [仓库授权与文件校验记录](unity/CancerTraceClient/Assets/Audio/BGM/SOURCE_LICENSE.md)

## 第三方软件依赖

- Unity、TextMesh Pro、Newtonsoft.Json 及其他 Package 的许可遵循各自 package 内 LICENSE；参见 `Packages/manifest.json`、`Packages/packages-lock.json`。本清单不替代依赖包自身 notices，正式分发前应核对实际导出依赖。
