/*  
	Class Name	: Comments.cs
	Description	: ����Ƽ Inspector �󿡼� �ڸ�Ʈ ������ ǥ���� 
	Author		: ���Ѽ�
	Since		: 2022.06.17
*/


using UnityEngine;
using System.Collections;

namespace HSCustom
{

    /// <summary>
    /// Adding comments to GameObjects in the Inspector.
    /// </summary>
    public class Comments : MonoBehaviour
    {

        /// <summary>
        /// The comment.
        /// </summary>
        [Multiline]
        public string text;
    }
}