/*  
	Class Name	: Comments.cs
	Description	: 유니티 Inspector 상에서 코멘트 내용을 표시함 
	Author		: 김한섭
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