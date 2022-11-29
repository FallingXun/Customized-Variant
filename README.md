# Customized-Variant

> 定制化变体工具。能对现有的预制体，定制一套对应的变体预制体。整套变体都沿用对应原预制体的信息，并且可以进行编辑修改。

## 简介 
- Unity 提供了变体机制，即对源预制体创建一个新的预制体（变体），新预制体（变体）只记录对源预制体的修改信息。对变体的所有修改，都不会影响源预制体，同时能沿用源预制体的所有信息。变体其实是对嵌套预制体的扩展，提供另一种差异化途径，其特点主要有：
    - 可以修改任意组件（Component）和任意物体（GameObject）的属性
    - 可以添加任意组件（AddComponent）
    - 可以移除任意组件（RemoveComponent）
    - 可以添加新的物体（AddGameObject）
- 另一方面，由于是嵌套预制机制的扩展，变体也有一定的限制（预览模式下）：
    - 不允许变更嵌套预制体的对象（同嵌套预制规则）
    - 不允许修改嵌套预制体的节点顺序（同嵌套预制体规则）
- 可以看出，如果源预制体中存在嵌套预制体，而嵌套预制体同样也有对应的变体，那么源预制体的变体是无法引用到嵌套预制体的变体的，也就是说，嵌套预制体的变体修改则无法和源预制体的变体关联起来。因此，要想定制一套对应的变体预制体，关键就是将嵌套预制体的变体修改应用到源预制体的变体上。
- 这里，先设定一系列概念：
    - xxx_CustomizedTemp.prefab : 临时变体，由 xxx.prefab 创建的变体
    - xxx_Customized.prefab ：定制变体，由 xxx_CustomizedTemp.prefab 创建的变体。

## 框架介绍
![Panel_A.png](/img/Unity/CustomizedVariant/Panel_A.png?raw=true)
- 以上图为例，Item_A 为 Panel_A 的嵌套预制体，RedPoint 为 Item_A 的嵌套预制体。当创建 Panel_A 的变体 Panel_A_Customized 时，将 Item_A 和 RedPoint 的变体（Item_A_Customized 、 RedPoint_Customized）信息也应用到 Panel_A_Customized 上，那么 Panel_A_Customized 就真正成为 Panel_A 对应的定制化变体预制体。此时，Panel_A_Customized 记录了 Item_A_Customized 、 RedPoint_Customized 、 Panel_A_Customized 的所有修改信息。
- 然而，当 Item_A_Customized 或 RedPoint_Customized 出现新的修改后，Panel_A_Customized 难以同步进行更新，因为不能分辨哪些修改信息是属于自身变体的，哪些是属于嵌套预制变体的。因此，需要将嵌套预制变体的修改信息分离出来，用 Panel_A_CustomizedTemp 来记录，则 Panel_A_Customized 只记录自身的修改信息。同理，由于 RedPoint 为 Item_A 的嵌套预制体，则 Item_A_CustomizedTemp 用来记录其修改，而 Item_A_Customized 只记录自身修改信息。
- 可以看出，通过分离后， xxx_CustomizedTemp.prefab 作为 xxx.prefab 的变体，记录所有嵌套预制的变体修改信息，而 xxx_Customized.prefab 作为 xxx_CustomizedTemp.prefab 的变体，只记录 xxx.prefab 的变体修改信息，从而实现了逐级分离，如下图所示：
![A.png](/img/Unity/CustomizedVariant/A.png?raw=true)
![B.png](/img/Unity/CustomizedVariant/B.png?raw=true)
![C.png](/img/Unity/CustomizedVariant/C.png?raw=true)

## 定制化变体
- 从上面可以知道，要想定制一套变体，首先要对应的预制体创建两个变体，xxx_CustomizedTemp.prefab 和 xxx_Customized.prefab 。
![Prefab.png](/img/Unity/CustomizedVariant/Prefab.png?raw=true)
- 其中，xxx_Customized 是想要的定制变体，可以像常规预制体一样，自由调整其信息。而 xxx_CustomizedTemp ，则需要将所有嵌套预制的变更应用上去。
- 然后，由于嵌套预制可能有多层，在常规预制体下，内层的预制修改不会影响到外层，而外层的预制修改可能会覆盖内层的修改，所以对于 xxx_CustomizedTemp 变体，需要自底向上，逐层获取嵌套预制体，将其 Customized 变体修改信息应用到 xxx_CustomizedTemp 上，保持和预制体同样的机制，保证了修改信息的准确性。
- 由简述中可以知道，定制变体的修改信息有四种类型，应用的时候有一定的顺序以及应用方式。

### 应用添加新的物体（AddGameObjects）
- 由于产生了新的对象，而对象可能被组件引用，所以新增的对象，需要最先应用，才能保证后续的引用关系能够正确赋值。
- 伪代码如下：
    - 实例化**根预制体** CustomizedTemp 变体对象
    - 获取**子预制体** Customized 变体对象所有新增的物体
    - 将新增的对象，实例化到根预制体实例对象对应节点上
    - 修改新增的对象名，保持根预制体实例对象上原有对象对应的名字
    - 将根预制体实例对象应用回根预制体上

### 应用移除组件（RemoveComponents）
- 当所有物体都加上后，就可以开始对组件进行操作。由于有的组件会产生互斥，如 Horizontal Layout Group 和 Vertical Layout Group ，两者同时只能存在一个，所以需要先移除旧的组件，才能保证新的组件能够正确应用上去。
- 伪代码如下：
    - 实例化**根预制体** CustomizedTemp 变体对象
    - 获取**子预制体** Customized 变体对象所有移除的组件
    - 找到根预制体实例上对应需要移除组件的物体，将组件移除
    - 将根预制体实例对象应用回根预制体上

### 应用添加组件（AddComponents）
- 处理完新增对象和移除组件后，就能进行添加组件的操作，对于子节点的引用，也能顺利进行。
- 伪代码如下：
    - 实例化**根预制体** CustomizedTemp 变体对象
    - 获取**子预制体** Customized 变体对象所有新增的组件
    - 找到根预制体实例上对应需要添加组件的物体，将组件添加上去
    - 将原组件的序列化数据，复制到新的组件上
    - 检查原组件的变量，如果引用的是子节点对象，则需要将新组件的引用，修改为其自身子节点的对应对象
    - 将根预制体实例对象应用回根预制体上

### 应用属性修改（PropertyModifications）
- 当所有增删修改都应用后，预制体的对象就已经齐全，就可以对组件或物体的属性变更进行应用。
- 伪代码如下：
    - 实例化**根预制体** CustomizedTemp 变体对象
    - 获取**子预制体** Customized 变体对象所有修改的属性
    - 剔除不进行应用的特殊属性
        - m_Name ：嵌套预制的名字修改，如果为添加 _CustomizedTemp 或 _Customized 后缀，则不需要应用
        - m_RootOrder ：节点顺序变化，由于嵌套预制体不允许修改子节点的位置，所以不需要应用
    - 如果修改的属性为子节点对象的引用，则需要将引用对象修改为根预制体中对应的对象
    - 将所有变更属性应用并保存

## 效果展示
![Compare_RedPoint.png](/img/Unity/CustomizedVariant/Compare_Redpoint.png?raw=true)
![Compare_Item_A.png](/img/Unity/CustomizedVariant/Compare_Item_A.png?raw=true)
![Compare_Panel_A.png](/img/Unity/CustomizedVariant/Compare_Panel_A.png?raw=true)

## 应用场景
- 定制化变体，可以用于开发中需要同时存在多套差异化预制体的情况，如：
    - 多语言版本差异化
    - 横竖屏版本差异化
    - ...

## 项目地址
[定制化变体 Customized-Variant](https://github.com/FallingXun/Customized-Variant.git)
