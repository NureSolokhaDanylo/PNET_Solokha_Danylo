import os
import re

pages_dir = "/home/nolax/Desktop/repos/PNET_Solokha_Danylo/PNET_Solokha_Danylo.Blazor/Components/Pages"

pagination_pattern = re.compile(r'<div class="d-flex flex-column flex-md-row align-items-center justify-content-between p-4 bg-light border-top gap-3">.*?</div>\s*</div>\s*</div>', re.DOTALL)
style_pattern = re.compile(r'<style>(.*?)</style>', re.DOTALL)

for filename in os.listdir(pages_dir):
    if not filename.endswith('.razor'):
        continue
    
    filepath = os.path.join(pages_dir, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content

    # Replace pagination
    if 'OnPageSizeChanged' in content and 'Showing <span' in content:
        # Find the footer pagination block
        # We replace the whole <div> block down to its closing tag with PaginationControl
        # Because the regex might be greedy or fail, we'll do a simpler replacement
        content = re.sub(r'<!-- Footer Pagination -->.*?</div>\s*</div>\s*</div>', 
                         '<PaginationControl TotalCount="totalCount" Skip="skip" PageSize="take" OnPageChanged="HandlePageChanged" />\n                        </div>\n                    </div>\n                </div>', 
                         content, flags=re.DOTALL)

    # Extract styles
    styles = style_pattern.findall(content)
    if styles:
        for style_content in styles:
            if '.header-card' in style_content or '.header-info' in style_content:
                # Need to save to CSS isolation
                css_filepath = filepath + '.css'
                with open(css_filepath, 'w', encoding='utf-8') as cf:
                    cf.write(style_content.strip() + "\n")
        
        # Remove style blocks entirely
        content = style_pattern.sub('', content)

    if content != original_content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Updated {filename}")

