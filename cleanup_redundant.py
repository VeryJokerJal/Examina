#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import re

def cleanup_redundant_parameters():
    file_path = "ExamLab/Services/ExcelKnowledgeService.cs"
    
    # Read the file with UTF-8 encoding
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    lines = content.split('\n')
    new_lines = []
    deleted_count = 0
    
    i = 0
    while i < len(lines):
        line = lines[i]
        
        # Check if current line contains "文本题目描述"
        if '文本题目描述' in line and 'DisplayName' in line:
            # Remove the comma from the previous line if it exists
            if new_lines and new_lines[-1].rstrip().endswith(','):
                new_lines[-1] = new_lines[-1].rstrip()[:-1]
            
            deleted_count += 1
            i += 1
            continue
        
        # Replace "目标图表" with "目标工作簿"
        line = line.replace('"目标图表"', '"目标工作簿"')
        
        new_lines.append(line)
        i += 1
    
    # Write back to file
    new_content = '\n'.join(new_lines)
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    
    print(f"Cleanup completed!")
    print(f"Original lines: {len(lines)}")
    print(f"New lines: {len(new_lines)}")
    print(f"Deleted parameters: {deleted_count}")
    
    # Check remaining "文本题目描述" occurrences
    remaining = new_content.count('文本题目描述')
    print(f"Remaining '文本题目描述' parameters: {remaining}")

if __name__ == "__main__":
    cleanup_redundant_parameters()
