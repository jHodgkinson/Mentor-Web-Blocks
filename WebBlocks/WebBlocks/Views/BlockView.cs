﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.HtmlControls;
using Umbraco.Core;
using Umbraco.Web;
using WebBlocks.Utilities.WebBlocks;
using umbraco.cms.businesslogic.macro;
using umbraco.cms.businesslogic.web;
using umbraco.NodeFactory;
using WebBlocks.BusinessLogic.Interfaces;
using WebBlocks.Views.RenderingEngines;
using WebBlocks.Model;
using CacheHelper = WebBlocks.Utilities.Cache.CacheHelper;
using umbraco.BusinessLogic;
using umbraco.presentation.preview;
using System.Xml;
using Umbraco.Core.Models;
using Umbraco.Web.Models;
using WebBlocks.Utilities.Umbraco;
using System.IO;
using System.Web.Mvc.Html;
using WebBlocks.API;

namespace WebBlocks.Views
{
    public class BlockView
    {
        /// <summary>
        /// Renders the block
        /// </summary>
        /// <returns>rendered html for the block</returns>
        public string Render(Block block, HtmlHelper html)
        {
            if (!HttpContext.Current.Response.IsClientConnected)
                return "";

            if (block is NodeBlock)
                return RenderNodeBlock(block as NodeBlock, html);
            else
                return RenderWysiwygBlock(block as WysiwygBlock, html);
        }

        protected string RenderNodeBlock(NodeBlock block, HtmlHelper html)
        {
            //check if the node exists
            if (block.Content == null) return null;

            //initialise WebBlocksAPI
            WebBlocksAPI blockInstanceAPI = new WebBlocksAPI();
            blockInstanceAPI.CssClasses = new List<string>();
            blockInstanceAPI.BlockElement = "";
            blockInstanceAPI.BlockAttributes = new Dictionary<string, string>();

            IRenderingEngine renderingEngine = ResolveRenderingEngine(block);

            //if rendering engine is null then the document doesn't exist anymore
            if (renderingEngine == null || block.Content == null) return "";

            //set the block model for the view to access
            WebBlocksUtility.CurrentBlockContent = block.Content;

            string blockIdAttribute = WebBlocksUtility.IsInBuilder ? string.Format(" wbid='{0}'", block.Id) : "";
            string blockTemplateAttribute = WebBlocksUtility.IsInBuilder ? 
                string.Format(" templateblock='{0}'", block.IsTemplateBlock.ToString().ToLower()) : "";
            string blockDeletedAttribute = WebBlocksUtility.IsInBuilder && block.IsDeleted ? " deletedBlock='deleted' style='display:none;visibilty:hidden;'" : "";

            string renderedContent = "";

            //render
            try
            {
                renderedContent = renderingEngine.Render(html);
            }
            catch(Exception ex)
            {
                renderedContent = HttpUtility.HtmlEncode(ex.ToString());
            }

            List<string> CssClasses = blockInstanceAPI.CssClasses;
            

            string blockElement = blockInstanceAPI.BlockElement ?? "div";
            blockElement = blockElement != "" ? blockElement : "div";

            Dictionary<string, string> blockAttributeDictionary = blockInstanceAPI.BlockAttributes;
            string blockAttributes = string.Join(" ", blockAttributeDictionary.Keys.Select(c => c + "='" + blockAttributeDictionary[c] + "'"));

            string blockClass = string.Format("{0}{1}{0}{2}{3}{4}", 
                block.Class.Length > 0 ? " " : "", 
                block.Class,
                WebBlocksUtility.CurrentBlockContent.GetPropertyValue("cssClasses"),
                CssClasses.Count() > 0 ? " " : "",
                String.Join(" ", CssClasses));
            blockClass = WebBlocksUtility.IsInBuilder ? "block " + blockClass : blockClass;

            renderedContent = string.Format("<{0} class='{1}'{2}{3}{4}{5}>{6}</{0}>", 
                blockElement, blockClass, blockIdAttribute, blockTemplateAttribute, blockDeletedAttribute, blockAttributes, renderedContent);

            return renderedContent;
        }

        protected string RenderWysiwygBlock(WysiwygBlock block, HtmlHelper html)
        {
            string webBlocksId = WebBlocksUtility.IsInBuilder ? string.Format(" wbid='{0}'", block.Id) : "";

            string blockClass = string.Format("pageWysiwygBlock{0}{1}", block.Class.Length > 0 ? " " : "", block.Class);

            blockClass = WebBlocksUtility.IsInBuilder ? "block " + blockClass : blockClass;

            string blockTemplateAttribute = WebBlocksUtility.IsInBuilder ? string.Format(" templateblock='{0}'", block.IsTemplateBlock.ToString().ToLower()) : "";
            string blockDeletedAttribute = WebBlocksUtility.IsInBuilder && block.IsDeleted ? " deletedBlock='deleted' style='display:none;visibilty:hidden;'" : "";

            WebBlocks.Helpers.TinyMCE tinyMceHelper = new Helpers.TinyMCE();
            Node currentNode = Node.GetCurrent();

            string blockContent = WebBlocksUtility.IsInBuilder ? block.Content : umbraco.library.RenderMacroContent(tinyMceHelper.ReplaceMacroTags(HttpUtility.UrlDecode(block.Content)), currentNode.Id);

            return string.Format("<{0}{1} class='{2}'{3}{4}>{5}</{0}>", block.Element, webBlocksId, blockClass, blockTemplateAttribute, blockDeletedAttribute,
                HttpUtility.UrlDecode(blockContent));
        }

        protected IRenderingEngine ResolveRenderingEngine(NodeBlock block)
        {
            try
            {
                Document blockDoc = new Document(block.Id);

                //resolve with partial view
                IRenderingEngine engine = new PartialViewRenderingEngine
                {
                    Macro = new MacroModel(new Macro { ScriptingFile = blockDoc.ContentType.Alias })
                };

                return engine;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RenderPreview(HtmlHelper html)
        {
            string htmlContent = Render(new NodeBlock(WebBlocksUtility.CurrentBlockContent.Id), html);

            html.ViewContext.Writer.Write(htmlContent);
        }
    }
}
