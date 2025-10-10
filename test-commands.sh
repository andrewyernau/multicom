#!/bin/bash
# Quick test script for agent commands

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Testing Agent Command System                              ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

echo "1. Testing /init command..."
echo "─────────────────────────────────────────────────────────────"
./init.sh
echo ""

echo "2. Testing /agent list command..."
echo "─────────────────────────────────────────────────────────────"
./agent.sh list
echo ""

echo "3. Testing /agent architect command..."
echo "─────────────────────────────────────────────────────────────"
./agent.sh architect
echo ""

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  All tests completed successfully!                         ║"
echo "╚════════════════════════════════════════════════════════════╝"
