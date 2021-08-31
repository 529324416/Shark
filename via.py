from pyecharts import options as opts
from pyecharts.charts import Tree
from pyecharts.options.charts_options import TreeMapLevelsOpts
from pyecharts.options.global_options import TitleOpts, ToolBoxFeatureOpts, ToolboxOpts

import json

def generate_from_jsonfile(filepath,output="render.html"):

    with open(filepath,"r",encoding='utf-8') as f:
        o = json.load(f)

    tree = Tree()
    tree.add(o['title'],[o['data']],collapse_interval=2,orient="TB",
    label_opts=opts.LabelOpts(
        position="top",
        horizontal_align="right",
        vertical_align="middle"
    ))
    tree.set_global_opts(title_opts=TitleOpts(title="ast generator testing",subtitle=o['title']))
    tree.render(output)

if __name__ == "__main__":
    
    generate_from_jsonfile("./ast.json")